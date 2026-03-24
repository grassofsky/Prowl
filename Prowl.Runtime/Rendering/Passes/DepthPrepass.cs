// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class DepthPrepass : RenderPass
{
    private RenderTexture _depthTexture;
    private RenderTexture _forwardBuffer;
    private readonly List<RenderTexture> _toRelease;

    public DepthPrepass()
    {
        Name = "Depth Prepass";
        InjectionPoint = RenderPassEvent.BeforeRenderingPrepasses;
        _toRelease = new List<RenderTexture>(1);
    }

    public void Setup(RenderTexture forwardBuffer)
    {
        _forwardBuffer = forwardBuffer;
    }

    private bool ShouldExecute(ref SRPRenderingData renderingData)
    {
        if (renderingData?.CameraData == null)
            return false;

        return renderingData.CameraData.DepthTextureMode.HasFlag(DepthTextureMode.Depth);
    }

    public override void Configure(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (!ShouldExecute(ref renderingData))
            return;

        var cameraData = renderingData.CameraData;

        _depthTexture = RenderTexture.GetTemporaryRT(
            cameraData.PixelWidth,
            cameraData.PixelHeight,
            new[] { PixelFormat.R32_Float });

        _toRelease.Clear();
        _toRelease.Add(_depthTexture);

        ConfigureTarget(_depthTexture);
        ConfigureClear(ClearFlag.All, Color.white);
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (!ShouldExecute(ref renderingData) || _depthTexture == null)
            return;

        var cmd = CommandBufferPool.Get(Name);

        SetRenderTarget(cmd);
        ClearRenderTarget(cmd);

        var drawingSettings = new DrawingSettings(
            new ShaderTagId("LightMode"),
            new SortingSettings(renderingData.CameraData));
        drawingSettings.SetShaderPassValue("ShadowCaster");

        var filteringSettings = new FilteringSettings(RenderQueueRange.Opaque);

        context.DrawRenderers(drawingSettings, filteringSettings);

        PropertyState.SetGlobalTexture("_CameraDepthTexture", _depthTexture);

        if (_forwardBuffer != null && _depthTexture?.DepthBuffer != null)
        {
            cmd.Blit(_depthTexture.DepthBuffer, _forwardBuffer, 1);
            cmd.SetRenderTarget(_forwardBuffer);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        foreach (var rt in _toRelease)
        {
            RenderTexture.ReleaseTemporaryRT(rt);
        }
        _toRelease.Clear();
        _depthTexture = null;
        _forwardBuffer = null;
    }
}
