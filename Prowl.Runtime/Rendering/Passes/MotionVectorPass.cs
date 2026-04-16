// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Numerics;

using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class MotionVectorPass : RenderPass
{
    private RenderTexture _colorTarget;
    private RenderTexture _motionVectorTexture;
    private Matrix4x4 _previousViewProj;

    public MotionVectorPass()
    {
        Name = "Motion Vector Pass";
        InjectionPoint = RenderPassEvent.AfterRenderingPrepasses;
    }

    public void Setup(RenderTexture colorTarget, Matrix4x4 previousViewProj)
    {
        _colorTarget = colorTarget;
        _previousViewProj = previousViewProj;
        _motionVectorTexture = null;
    }

    public RenderTexture GetMotionVectorTexture()
    {
        return _motionVectorTexture;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (renderingData?.CameraData == null || _colorTarget == null)
            return;

        var cameraData = renderingData.CameraData;

        if (!cameraData.DepthTextureMode.HasFlag(DepthTextureMode.MotionVectors))
            return;

        var cmd = CommandBufferPool.Get(Name);

        uint width = cameraData.PixelWidth;
        uint height = cameraData.PixelHeight;

        _motionVectorTexture = RenderTexture.GetTemporaryRT(width, height, [PixelFormat.R16_G16_Float]);

        cmd.SetRenderTarget(_motionVectorTexture);
        cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        cmd.SetViewports(0, 0, (int)width, (int)height, 0, 1);

        cmd.SetMatrix("prowl_PrevViewProj", _previousViewProj.ToFloat());

        var drawingSettings = new DrawingSettings(
            new ShaderTagId("LightMode"),
            new SortingSettings(cameraData));
        drawingSettings.SetShaderPassValue("MotionVectors");
        drawingSettings.SetPerObjectData(PerObjectData.MotionVectors);

        var filteringSettings = new FilteringSettings(
            RenderQueueRange.All,
            cameraData.CullingMask);

        RenderUtils.SetGlobalCameraMatrices(cameraData.ViewMatrix, cameraData.ProjectionMatrix);

        context.DrawRenderers(drawingSettings, filteringSettings, cmd);

        PropertyState.SetGlobalTexture("_CameraMotionVectorsTexture", _motionVectorTexture);

        Graphics.SubmitCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (_motionVectorTexture != null)
        {
            RenderTexture.ReleaseTemporaryRT(_motionVectorTexture);
            _motionVectorTexture = null;
        }
        _colorTarget = null;
    }
}
