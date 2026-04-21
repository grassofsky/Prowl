// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Veldrid;

namespace Prowl.Runtime.Rendering.NativeRendering;

/// <summary>
/// Provides access to the underlying graphics device and native handles for interop with native rendering plugins.
/// Analogous to Unity's IUnityGraphics + IUnityGraphicsD3D11 + IUnityGraphicsVulkan combined into a single C# interface.
/// </summary>
public interface IProwlGraphicsInterop
{
    /// <summary>The active graphics backend type.</summary>
    GraphicsBackend BackendType { get; }

    /// <summary>
    /// Returns the native device handle for the current backend.
    /// D3D11: ID3D11Device*, Vulkan: VkDevice, OpenGL/Metal: IntPtr.Zero (not applicable).
    /// </summary>
    nint GetDeviceHandle();

    /// <summary>
    /// Returns the native adapter/physical device handle.
    /// D3D11: IDXGIAdapter*, Vulkan: VkPhysicalDevice, others: IntPtr.Zero.
    /// </summary>
    nint GetAdapterHandle();

    /// <summary>
    /// Returns the Vulkan instance handle. Zero on non-Vulkan backends.
    /// </summary>
    nint GetInstanceHandle();

    /// <summary>
    /// Returns the graphics command queue handle.
    /// Vulkan: VkQueue, others: IntPtr.Zero.
    /// </summary>
    nint GetCommandQueueHandle();

    /// <summary>
    /// Returns the Vulkan graphics queue family index. Zero on non-Vulkan backends.
    /// </summary>
    uint GetQueueFamilyIndex();

    /// <summary>
    /// Whether the specified backend is supported for native rendering interop.
    /// Metal is currently not supported.
    /// </summary>
    bool IsBackendSupported(GraphicsBackend backend);

    /// <summary>Fired after the graphics device is created and ready for use.</summary>
    event Action? DeviceCreated;

    /// <summary>Fired before the graphics device is destroyed.</summary>
    event Action? DeviceDestroying;
}
