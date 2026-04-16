// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class TransparentRenderPass : RenderPass
{
    public TransparentRenderPass()
    {
        Name = "Draw Transparent";
        InjectionPoint = RenderPassEvent.AfterRenderingOpaques;
    }

    public void Setup(RenderTexture colorTarget, RenderTexture depthTarget)
    {
        ConfigureTarget(colorTarget, depthTarget);
    }

    public override void Configure(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (_colorTarget == null || _depthTarget == null)
            return;

        ConfigureTarget(_colorTarget, _depthTarget);
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (renderingData?.CameraData == null)
            return;

        var cameraData = renderingData.CameraData;

        var cmd = CommandBufferPool.Get(Name);

        SetRenderTarget(cmd);
        cmd.SetViewports(0, 0, (int)cameraData.PixelWidth, (int)cameraData.PixelHeight, 0, 1);

        var drawingSettings = new DrawingSettings(
            new ShaderTagId("RenderOrder"),
            new SortingSettings(cameraData, SortingCriteria.BackToFront));
        drawingSettings.SetShaderPassValue("Transparent");

        var filteringSettings = new FilteringSettings(
            RenderQueueRange.Transparent,
            cameraData.CullingMask);

        RenderUtils.SetGlobalCameraMatrices(cameraData.ViewMatrix, cameraData.ProjectionMatrix);

        context.DrawRenderers(drawingSettings, filteringSettings, cmd);

        Graphics.SubmitCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        _colorTarget = null;
        _depthTarget = null;
    }
}
