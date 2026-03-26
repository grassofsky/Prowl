// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Numerics;

using Prowl.Runtime.Rendering.Pipelines;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public class CullingResults
{
    private readonly List<IRenderable> _visibleRenderables;
    private readonly Dictionary<Material, RenderBatch> _batches;
    private readonly List<IRenderableLight> _visibleLights;
    private readonly List<(int index, float distance)> _sortBuffer;

    public int VisibleObjectCount => _visibleRenderables.Count;
    public int VisibleLightCount => _visibleLights.Count;

    public CullingResults()
    {
        _visibleRenderables = new List<IRenderable>();
        _batches = new Dictionary<Material, RenderBatch>();
        _visibleLights = new List<IRenderableLight>();
        _sortBuffer = new List<(int, float)>();
    }

    public IEnumerable<IRenderable> GetVisibleRenderers() => _visibleRenderables;
    public IEnumerable<RenderBatch> GetBatches() => _batches.Values;
    public IEnumerable<IRenderableLight> GetVisibleLights() => _visibleLights;

    public void DrawRenderers(ScriptableRenderContext context, DrawingSettings drawingSettings, FilteringSettings filteringSettings)
    {
        var cmd = CommandBufferPool.Get("DrawRenderers");

        Vector3 cameraPosition = drawingSettings.SortingSettings.CameraPosition;

        foreach (var batch in _batches.Values)
        {
            if (batch.material == null || batch.material.Shader.IsAvailable == false)
                continue;

            if (!PassesFilter(batch.material, filteringSettings))
                continue;

            var shader = batch.material.Shader.Res;

            foreach (var pass in shader.Passes)
            {
                bool hasTag = drawingSettings.ShaderPassNames != null && drawingSettings.ShaderPassNames.Length > 0;
                if (hasTag)
                {
                    bool passMatches = false;
                    for (int t = 0; t < drawingSettings.ShaderPassNames.Length; t++)
                    {
                        string tagName = drawingSettings.ShaderPassNames[t].Name;
                        if (drawingSettings.ShaderPassValue != null)
                        {
                            if (pass.HasTag(tagName, drawingSettings.ShaderPassValue))
                            {
                                passMatches = true;
                                break;
                            }
                        }
                        else
                        {
                            if (pass.HasTag(tagName))
                            {
                                passMatches = true;
                                break;
                            }
                        }
                    }
                    if (!passMatches)
                        continue;
                }

                cmd.ApplyPropertyState(batch.material._properties);
                cmd.SetPass(pass);
                cmd.BindResources();

                var sortedIndices = GetSortedIndices(batch.renderIndices, drawingSettings.SortingSettings);

                foreach (int renderIndex in sortedIndices)
                {
                    if (renderIndex < 0 || renderIndex >= _visibleRenderables.Count)
                        continue;

                    var renderable = _visibleRenderables[renderIndex];

                    if (!PassesLayerFilter(renderable, filteringSettings))
                        continue;

                    renderable.GetRenderingData(out PropertyState properties, out IGeometryDrawData drawData, out Matrix4x4 model);

                    cmd.ApplyPropertyState(properties);

                    if (drawingSettings.PerObjectData.HasFlag(PerObjectData.MotionVectors))
                    {
                        if (properties.TryGetInt("_ObjectID", out int instanceId))
                        {
                            TrackModelMatrix(cmd, instanceId, model);
                        }
                    }

                    model.Translation -= cameraPosition;

                    cmd.SetMatrix("prowl_ObjectToWorld", model.ToFloat());
                    cmd.SetMatrix("prowl_WorldToObject", model.Invert().ToFloat());
                    cmd.SetColor("_MainColor", Color.white);
                    cmd.UpdateBuffer("_PerDraw");

                    cmd.SetDrawData(drawData);
                    cmd.DrawIndexed((uint)drawData.IndexCount, 0, 1, 0, 0);
                }
            }
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private static readonly Dictionary<int, Matrix4x4> s_prevModelMatrices = new();
    private static readonly HashSet<int> s_activeObjectIds = new();
    private static int s_framesSinceLastCleanup = 0;
    private const int CLEANUP_INTERVAL_FRAMES = 120;

    private static void TrackModelMatrix(CommandBuffer buffer, int objectId, Matrix4x4 currentModel)
    {
        s_activeObjectIds.Add(objectId);

        if (s_prevModelMatrices.TryGetValue(objectId, out Matrix4x4 prevModel))
            buffer.SetMatrix("prowl_PrevObjectToWorld", prevModel.ToFloat());
        else
            buffer.SetMatrix("prowl_PrevObjectToWorld", currentModel.ToFloat());

        s_prevModelMatrices[objectId] = currentModel;
    }

    public static void CleanupUnusedModelMatrices()
    {
        s_framesSinceLastCleanup++;

        if (s_framesSinceLastCleanup < CLEANUP_INTERVAL_FRAMES)
            return;

        s_framesSinceLastCleanup = 0;

        var unusedKeys = new List<int>();
        foreach (var key in s_prevModelMatrices.Keys)
        {
            if (!s_activeObjectIds.Contains(key))
                unusedKeys.Add(key);
        }

        foreach (int key in unusedKeys)
            s_prevModelMatrices.Remove(key);

        s_activeObjectIds.Clear();
    }

    private bool PassesFilter(Material material, FilteringSettings filtering)
    {
        if (material == null || material.Shader.IsAvailable == false)
            return false;

        return true;
    }

    private bool PassesLayerFilter(IRenderable renderable, FilteringSettings filtering)
    {
        int layer = renderable.GetLayer();
        return filtering.LayerMask.HasLayer(layer);
    }

    private List<int> GetSortedIndices(List<int> indices, SortingSettings sorting)
    {
        if (sorting.Criteria == SortingCriteria.None)
            return indices;

        _sortBuffer.Clear();
        _sortBuffer.EnsureCapacity(indices.Count);

        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            if (idx >= 0 && idx < _visibleRenderables.Count)
            {
                _visibleRenderables[idx].GetCullingData(out _, out var bounds);
                Vector3 diff = sorting.CameraPosition - new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
                float distSq = (float)(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z);
                _sortBuffer.Add((idx, distSq));
            }
        }

        if (sorting.Criteria.HasFlag(SortingCriteria.BackToFront))
        {
            _sortBuffer.Sort((a, b) => b.distance.CompareTo(a.distance));
        }
        else if (sorting.Criteria.HasFlag(SortingCriteria.FrontToBack))
        {
            _sortBuffer.Sort((a, b) => a.distance.CompareTo(b.distance));
        }

        var result = new List<int>(_sortBuffer.Count);
        for (int i = 0; i < _sortBuffer.Count; i++)
        {
            result.Add(_sortBuffer[i].index);
        }

        return result;
    }

    internal void AddRenderable(IRenderable renderable)
    {
        _visibleRenderables.Add(renderable);

        var material = renderable.GetMaterial();
        if (material == null)
            return;

        if (!_batches.TryGetValue(material, out var batch))
        {
            batch = new RenderBatch
            {
                material = material,
                renderIndices = new List<int>()
            };
            _batches[material] = batch;
        }
        batch.renderIndices.Add(_visibleRenderables.Count - 1);
    }

    internal void AddLight(IRenderableLight light)
    {
        _visibleLights.Add(light);
    }

    internal void Clear()
    {
        _visibleRenderables.Clear();
        _batches.Clear();
        _visibleLights.Clear();
        _sortBuffer.Clear();
    }

    public void Cull(CameraData cameraData, IEnumerable<IRenderable> allRenderables, IEnumerable<IRenderableLight> allLights)
    {
        Clear();

        if (allRenderables != null)
        {
            foreach (var renderable in allRenderables)
            {
                renderable.GetCullingData(out bool isRenderable, out Bounds bounds);

                if (!isRenderable)
                    continue;

                if (!cameraData.CullingMask.HasLayer(renderable.GetLayer()))
                    continue;

                if (!cameraData.WorldFrustum.Intersects(bounds))
                    continue;

                AddRenderable(renderable);
            }
        }

        if (allLights != null)
        {
            foreach (var light in allLights)
            {
                AddLight(light);
            }
        }
    }

    public static CullingResults PerformCulling(
        CameraData cameraData,
        IEnumerable<IRenderable> allRenderables,
        IEnumerable<IRenderableLight> allLights)
    {
        var results = new CullingResults();
        results.Cull(cameraData, allRenderables, allLights);
        return results;
    }
}

public static class IRenderableExtensions
{
    public static Bounds GetCullingDataBounds(this IRenderable renderable)
    {
        renderable.GetCullingData(out _, out Bounds bounds);
        return bounds;
    }
}
