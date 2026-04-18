// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Prowl.Echo;
using Prowl.Runtime.Rendering.Pipelines;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public abstract class RenderPipelineAsset : ScriptableObject
{
    [SerializeField]
    protected List<RenderFeature> _renderFeatures = new();

    [SerializeField]
    protected PipelineSettings _settings = new();

    public override void OnEnable()
    {
        base.OnEnable();

        // Initialize fields if null (after deserialization)
        _renderFeatures ??= new List<RenderFeature>();
        _settings ??= new PipelineSettings();

        // Initialize non-serialized fields
        _rendererLock ??= new object();
        _contextLock ??= new object();
        _cameraContexts ??= new ConditionalWeakTable<Camera, ScriptableRenderContext>();
    }

    [SerializeIgnore]
    private volatile ScriptableRenderer _sharedRenderer;

    [SerializeIgnore]
    private object _rendererLock = new object();

    // Per-camera context storage for thread safety
    [SerializeIgnore]
    private ConditionalWeakTable<Camera, ScriptableRenderContext> _cameraContexts = new();

    [SerializeIgnore]
    private object _contextLock = new object();

    public IReadOnlyList<RenderFeature> RenderFeatures => _renderFeatures;
    public PipelineSettings Settings => _settings;

    public abstract ScriptableRenderer CreateRenderer();

    public ScriptableRenderer GetSharedRenderer()
    {
        if (_sharedRenderer != null)
            return _sharedRenderer;

        lock (_rendererLock)
        {
            if (_sharedRenderer == null)
            {
                _sharedRenderer = CreateRenderer();
                SetupRendererFeatures(_sharedRenderer);
            }
        }

        return _sharedRenderer;
    }

    public ScriptableRenderContext GetContextForCamera(Camera camera)
    {
        if (camera == null)
            throw new ArgumentNullException(nameof(camera));

        // Fast path: check if context already exists
        if (_cameraContexts.TryGetValue(camera, out var context))
            return context;

        // Slow path: create new context
        lock (_contextLock)
        {
            // Double-check after acquiring lock
            if (_cameraContexts.TryGetValue(camera, out context))
                return context;

            context = new ScriptableRenderContext();
            _cameraContexts.Add(camera, context);
            return context;
        }
    }

    public void CleanupCameraContext(Camera camera)
    {
        if (camera == null)
            return;

        lock (_contextLock)
        {
            if (_cameraContexts.TryGetValue(camera, out var context))
            {
                context?.Dispose();
                _cameraContexts.Remove(camera);
            }
        }

        // Also cleanup renderer's camera-specific resources
        _sharedRenderer?.CleanupCamera(camera);
    }

    public virtual RenderPipeline CreatePipeline()
    {
        return new SRPRenderPipeline(this);
    }

    public void AddRenderFeature(RenderFeature feature)
    {
        _renderFeatures.Add(feature);
        InvalidateSharedRenderer();
    }

    public void RemoveRenderFeature(RenderFeature feature)
    {
        _renderFeatures.Remove(feature);
        InvalidateSharedRenderer();
    }

    public void RemoveRenderFeatureAt(int index)
    {
        _renderFeatures.RemoveAt(index);
        InvalidateSharedRenderer();
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

    public void InvalidateSharedRenderer()
    {
        lock (_rendererLock)
        {
            if (_sharedRenderer != null)
            {
                _sharedRenderer.Dispose();
                _sharedRenderer = null;
            }
        }

        // Cleanup all per-camera contexts
        lock (_contextLock)
        {
            foreach (var kvp in _cameraContexts)
            {
                kvp.Value?.Dispose();
            }
            _cameraContexts.Clear();
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
        _renderer = asset.GetSharedRenderer();
    }

    public override void Render(Camera camera, in RenderingData data)
    {
        Framebuffer cameraTarget = camera.UpdateRenderData();

        // 1. Pre Cull - Call OnPreCull for all MonoBehaviour components on the camera
        // This allows effects like MotionBlurEffect to set DepthTextureMode before CameraData is created
        var components = camera.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component.EnabledInHierarchy)
                component.OnPreCull(camera);
        }

        var srpData = SRPRenderingData.Create(camera, data.IsSceneViewCamera);
        srpData.CameraTarget = cameraTarget;
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

        // 2. Pre Render - Call OnPreRender after CameraData is created
        foreach (var component in components)
        {
            if (component != null && component.EnabledInHierarchy)
                component.OnPreRender(camera);
        }

        var context = _asset.GetContextForCamera(camera);
        context.SetCullingResults(cullingResults);
        _renderer.Render(camera, srpData, context);
    }
}
