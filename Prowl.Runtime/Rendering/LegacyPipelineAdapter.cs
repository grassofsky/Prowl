// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Linq;

using Prowl.Runtime.Rendering.Pipelines;

namespace Prowl.Runtime.Rendering;

[Obsolete("Use DefaultRenderPipelineAsset with ForwardRenderer instead. See Prowl.Runtime.Rendering namespace.")]
public class LegacyPipelineAdapter : RenderPipeline
{
    private ForwardRenderer _renderer;
    private ScriptableRenderContext _context;

    public LegacyPipelineAdapter()
    {
        _renderer = null;
        _context = null;
    }

    public void Initialize()
    {
        if (_renderer == null)
        {
            var settings = new PipelineSettings();
            _renderer = new ForwardRenderer(settings);
            _context = new ScriptableRenderContext();
        }
    }

    public override void Render(Camera camera, in RenderingData data)
    {
        Initialize();

        var cameraData = CameraData.Create(camera, data.IsSceneViewCamera);

        var cullingResults = CullingResults.PerformCulling(
            cameraData,
            RenderPipeline.GetRenderables(),
            RenderPipeline.GetLights());

        var srpRenderingData = new SRPRenderingData
        {
            CameraData = cameraData,
            CullingResults = cullingResults,
            LightData = new SRPLightData { VisibleLights = cullingResults.GetVisibleLights().ToList() },
            ShadowData = new SRPShadowData(),
            PostProcessingData = new SRPPostProcessingData(),
            IsSceneViewCamera = data.IsSceneViewCamera,
            DisplayGrid = data.DisplayGrid,
            DisplayGizmo = data.DisplayGizmo,
            GridMatrix = data.GridMatrix,
            GridColor = data.GridColor,
            GridSizes = data.GridSizes
        };

        _context.Setup(camera, in srpRenderingData);

        _renderer.Setup(_context, ref srpRenderingData);
        _renderer.Execute(_context, ref srpRenderingData);

        _context.Submit();
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _context?.Dispose();
        _context = null;
    }
}
