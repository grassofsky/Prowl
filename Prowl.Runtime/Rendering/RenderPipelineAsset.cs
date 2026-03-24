// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Prowl.Echo;
using Prowl.Runtime.Rendering.Pipelines;

namespace Prowl.Runtime.Rendering;

public abstract class RenderPipelineAsset : EngineObject
{
    [SerializeField]
    protected List<RenderFeature> _renderFeatures = new();

    [SerializeField]
    protected PipelineSettings _settings = new();

    public IReadOnlyList<RenderFeature> RenderFeatures => _renderFeatures;
    public PipelineSettings Settings => _settings;

    public abstract ScriptableRenderer CreateRenderer();

    public virtual RenderPipeline CreatePipeline()
    {
        return new SRPRenderPipeline(this);
    }

    public void AddRenderFeature(RenderFeature feature)
    {
        _renderFeatures.Add(feature);
    }

    public void RemoveRenderFeature(RenderFeature feature)
    {
        _renderFeatures.Remove(feature);
    }

    public void RemoveRenderFeatureAt(int index)
    {
        _renderFeatures.RemoveAt(index);
    }

    public virtual bool Validate()
    {
        foreach (var feature in _renderFeatures)
        {
            if (feature == null)
                return false;
            if (!feature.Validate())
                return false;
        }
        return true;
    }

    public virtual PipelineSettings GetDefaultSettings()
    {
        return new PipelineSettings();
    }

    internal void SetupRendererFeatures(ScriptableRenderer renderer)
    {
        foreach (var feature in _renderFeatures)
        {
            if (feature != null && feature.Enabled)
            {
                renderer.AddFeature(feature);
            }
        }
    }
}

public class PipelineSettings
{
    public bool UseHDR = true;
    public int MaxLights = 16;
    public int ShadowAtlasSize = 4096;
    public bool UseDepthPrepass = true;
    public bool UseMotionVectors = false;
}

internal class SRPRenderPipeline : RenderPipeline
{
    private readonly RenderPipelineAsset _asset;
    private ScriptableRenderer _renderer;

    public SRPRenderPipeline(RenderPipelineAsset asset)
    {
        _asset = asset;
        _renderer = asset.CreateRenderer();
        _asset.SetupRendererFeatures(_renderer);
    }

    public override void Render(Camera camera, in RenderingData data)
    {
        var srpData = SRPRenderingData.Create(camera, data.IsSceneViewCamera);
        srpData.DisplayGrid = data.DisplayGrid;
        srpData.DisplayGizmo = data.DisplayGizmo;
        srpData.GridMatrix = data.GridMatrix;
        srpData.GridColor = data.GridColor;
        srpData.GridSizes = data.GridSizes;

        var cullingResults = CullingResults.PerformCulling(
            srpData.CameraData,
            RenderPipeline.GetRenderables(),
            RenderPipeline.GetLights());

        srpData.CullingResults = cullingResults;

        _renderer.Render(camera, srpData);
    }
}
