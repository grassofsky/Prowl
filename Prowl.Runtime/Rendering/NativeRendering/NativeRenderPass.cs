// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Runtime.Rendering.NativeRendering;

/// <summary>
/// A render pass that invokes a managed callback at a specified point in the SRP pipeline.
/// Analogous to Unity's CommandBuffer.IssuePluginEvent, but using a C# delegate instead of C ABI.
/// You can implement custom native render pass refer to <see cref="NativeRenderFeature"/> and the example in its documentation.
/// </summary>
public sealed class NativeRenderPass : RenderPass
{
    private readonly Action<NativeRenderContext> _callback;
    private readonly int _eventId;

    /// <summary>
    /// Creates a new NativeRenderPass.
    /// </summary>
    /// <param name="name">Display name for this pass.</param>
    /// <param name="injectionPoint">When in the pipeline this pass should execute.</param>
    /// <param name="callback">The callback invoked during execution. Must not be null.</param>
    /// <param name="eventId">User-defined event identifier passed through to the callback context.</param>
    public NativeRenderPass(string name, RenderPassEvent injectionPoint, Action<NativeRenderContext> callback, int eventId = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        InjectionPoint = injectionPoint;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _eventId = eventId;
    }

    /// <summary>
    /// Configures the color target for this pass. Call from RenderFeature.AddRenderPasses
    /// each frame before enqueueing, following the same pattern as built-in passes.
    /// </summary>
    public void Setup(RenderTexture colorTarget)
    {
        ConfigureTarget(colorTarget);
    }

    /// <summary>
    /// Configures both color and depth targets for this pass.
    /// </summary>
    public void Setup(RenderTexture colorTarget, RenderTexture depthTarget)
    {
        ConfigureTarget(colorTarget, depthTarget);
    }

    /// <summary>
    /// Returns the color render target configured via <see cref="Setup(RenderTexture)"/>.
    /// </summary>
    public RenderTexture GetColorTarget() => _colorTarget;

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        var interop = Graphics.GraphicsInterop;
        var nativeContext = new NativeRenderContext
        {
            BackendType = interop?.BackendType ?? Veldrid.GraphicsBackend.Null,
            DeviceHandle = interop?.GetDeviceHandle() ?? 0,
            EventId = _eventId,
            RenderingData = renderingData,
        };

        try
        {
            _callback(nativeContext);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NativeRenderPass] Exception in '{Name}': {ex}");
        }
    }
}
