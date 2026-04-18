// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Veldrid;

namespace Prowl.Runtime.Rendering;

public abstract class RenderPass
{
    public string Name { get; protected set; } = "Unnamed Pass";
    public RenderPassEvent InjectionPoint { get; protected set; } = RenderPassEvent.AfterRendering;
    internal int EnqueueOrder { get; set; }

    public RenderTargetIdentifier ColorTarget { get; protected set; }
    public RenderTargetIdentifier DepthTarget { get; protected set; }

    protected ClearFlag _clearFlag = ClearFlag.None;
    protected Color _clearColor = Color.black;

    protected RenderTexture _colorTarget;
    protected RenderTexture _depthTarget;

    public virtual void Configure(ScriptableRenderContext context, ref SRPRenderingData renderingData) { }

    public abstract void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData);

    public virtual void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData) { }

    internal void ReleaseCombinedFramebuffer()
    {
        _combinedFramebuffer?.Dispose();
        _combinedFramebuffer = null;
        _cachedColorForCombined = null;
        _cachedDepthForCombined = null;
    }

    protected void ConfigureTarget(RenderTargetIdentifier colorTarget)
    {
        ColorTarget = colorTarget;
    }

    protected void ConfigureTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
    {
        ColorTarget = colorTarget;
        DepthTarget = depthTarget;
    }

    protected void ConfigureTarget(RenderTexture colorTarget)
    {
        _colorTarget = colorTarget;
        ColorTarget = new RenderTargetIdentifier(colorTarget);
    }

    protected void ConfigureTarget(RenderTexture colorTarget, RenderTexture depthTarget)
    {
        _colorTarget = colorTarget;
        _depthTarget = depthTarget;
        ColorTarget = new RenderTargetIdentifier(colorTarget);
        DepthTarget = new RenderTargetIdentifier(depthTarget);
    }

    protected void ConfigureClear(ClearFlag clearFlag, Color clearColor)
    {
        _clearFlag = clearFlag;
        _clearColor = clearColor;
    }

    private Framebuffer _combinedFramebuffer;
    private RenderTexture _cachedColorForCombined;
    private RenderTexture _cachedDepthForCombined;

    protected void SetRenderTarget(CommandBuffer cmd)
    {
        if (_colorTarget != null && _depthTarget != null)
        {
            if (_depthTarget != _colorTarget)
            {
                // Rebuild combined framebuffer only when targets change
                if (_combinedFramebuffer == null ||
                    _cachedColorForCombined != _colorTarget ||
                    _cachedDepthForCombined != _depthTarget)
                {
                    _combinedFramebuffer?.Dispose();

                    var colorBuffers = _colorTarget.ColorBuffers;
                    var textures = new Veldrid.Texture[colorBuffers.Length];
                    for (int i = 0; i < colorBuffers.Length; i++)
                        textures[i] = colorBuffers[i].InternalTexture;

                    var desc = new FramebufferDescription(
                        _depthTarget.DepthBuffer?.InternalTexture,
                        textures);
                    _combinedFramebuffer = Graphics.Factory.CreateFramebuffer(desc);
                    _cachedColorForCombined = _colorTarget;
                    _cachedDepthForCombined = _depthTarget;
                }

                cmd.SetRenderTarget(_combinedFramebuffer);
            }
            else
            {
                cmd.SetRenderTarget(_colorTarget);
            }
        }
        else if (_colorTarget != null)
        {
            cmd.SetRenderTarget(_colorTarget);
        }
        else if (_depthTarget != null)
        {
            cmd.SetRenderTarget(_depthTarget);
        }
    }

    protected void ClearRenderTarget(CommandBuffer cmd)
    {
        if (_colorTarget != null || _depthTarget != null)
        {
            bool clearDepth = _clearFlag.HasFlag(ClearFlag.Depth);
            bool clearColor = _clearFlag.HasFlag(ClearFlag.Color);
            cmd.ClearRenderTarget(clearDepth, clearColor, _clearColor);
        }
    }
}
