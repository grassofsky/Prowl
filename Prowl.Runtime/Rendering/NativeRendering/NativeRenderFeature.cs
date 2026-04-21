// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

namespace Prowl.Runtime.Rendering.NativeRendering;

/// <summary>
/// A render feature that registers one or more <see cref="NativeRenderPass"/> callbacks into the SRP pipeline.
/// Analogous to a Unity ScriptableRendererFeature that injects native plugin events.
/// You can implement custom native render feature refer to NativeRenderFeature and the example in its documentation.
/// </summary>
/// <example>
/// <code>
/// var feature = new NativeRenderFeature("My Native Effect");
/// feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, ctx =>
/// {
///     // Use ctx.DeviceHandle to call native graphics APIs
///     MyNativePlugin.RenderEffect(ctx.DeviceHandle, ctx.EventId);
/// }, eventId: 42);
/// </code>
/// </example>
public class NativeRenderFeature : RenderFeature
{
    private readonly List<NativeRenderPass> _passes = new();

    /// <summary>
    /// Creates a new NativeRenderFeature with the default name.
    /// </summary>
    public NativeRenderFeature() : this("Native Render Feature") { }

    /// <summary>
    /// Creates a new NativeRenderFeature with the given name.
    /// </summary>
    public NativeRenderFeature(string featureName)
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// Registers a callback to be executed at the specified pipeline injection point.
    /// </summary>
    /// <param name="injectionPoint">When in the pipeline the callback should execute.</param>
    /// <param name="callback">The callback to invoke. Receives a <see cref="NativeRenderContext"/> with device handles and event data.</param>
    /// <param name="eventId">Optional user-defined event ID passed to the callback.</param>
    /// <param name="passName">Optional display name for the pass. Defaults to the feature name + event ID.</param>
    public void AddCallback(RenderPassEvent injectionPoint, Action<NativeRenderContext> callback, int eventId = 0, string? passName = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var pass = new NativeRenderPass(
            passName ?? $"{FeatureName}_Event{eventId}",
            injectionPoint, callback, eventId);
        _passes.Add(pass);
    }

    /// <summary>
    /// Removes all registered callbacks.
    /// </summary>
    public void ClearCallbacks() => _passes.Clear();

    public override void AddRenderPasses(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        for (int i = 0; i < _passes.Count; i++)
            context.EnqueuePass(_passes[i]);
    }
}
