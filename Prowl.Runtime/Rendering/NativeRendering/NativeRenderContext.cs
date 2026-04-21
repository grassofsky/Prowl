// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Veldrid;

namespace Prowl.Runtime.Rendering.NativeRendering;

/// <summary>
/// Context data passed to native render plugin callbacks during execution.
/// Analogous to the data Unity provides through IUnityGraphics callback parameters.
/// </summary>
public readonly struct NativeRenderContext
{
    /// <summary>The active graphics backend type.</summary>
    public GraphicsBackend BackendType { get; init; }

    /// <summary>
    /// The native device handle (D3D11: ID3D11Device*, Vulkan: VkDevice).
    /// </summary>
    public nint DeviceHandle { get; init; }

    /// <summary>
    /// User-defined event ID, analogous to the eventId parameter in Unity's GL.IssuePluginEvent.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// The rendering data for the current camera/frame.
    /// </summary>
    public SRPRenderingData RenderingData { get; init; }
}
