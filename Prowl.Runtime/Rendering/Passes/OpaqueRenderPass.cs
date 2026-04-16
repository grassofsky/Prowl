// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class OpaqueRenderPass : RenderPass
{
    public OpaqueRenderPass()
    {
        Name = "Draw Opaque";
        InjectionPoint = RenderPassEvent.AfterRenderingPrepasses;
    }

    public void Setup(RenderTexture colorTarget, RenderTexture depthTarget, CameraClearFlags clearFlags, Color clearColor)
    {
        ConfigureTarget(colorTarget, depthTarget);
        
        bool clearColorBuffer = clearFlags == CameraClearFlags.ColorOnly || clearFlags == CameraClearFlags.DepthColor;
        bool clearDepthBuffer = clearFlags == CameraClearFlags.DepthOnly || clearFlags == CameraClearFlags.DepthColor;
        bool drawSkybox = clearFlags == CameraClearFlags.Skybox;
        
        if (clearColorBuffer || clearDepthBuffer || drawSkybox)
        {
            ClearFlag flag = ClearFlag.None;
            if (clearDepthBuffer || drawSkybox)
                flag |= ClearFlag.Depth;
            if (clearColorBuffer || drawSkybox)
                flag |= ClearFlag.Color;
            ConfigureClear(flag, clearColor);
        }
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
        ClearRenderTarget(cmd);
        cmd.SetViewports(0, 0, (int)cameraData.PixelWidth, (int)cameraData.PixelHeight, 0, 1);

        var drawingSettings = new DrawingSettings(
            new ShaderTagId("RenderOrder"),
            new SortingSettings(cameraData, SortingCriteria.FrontToBack));
        drawingSettings.SetShaderPassValue("Opaque");

        var filteringSettings = new FilteringSettings(
            RenderQueueRange.Opaque,
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
