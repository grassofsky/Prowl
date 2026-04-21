// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Veldrid;

namespace Prowl.Runtime.Rendering.NativeRendering;

/// <summary>
/// Concrete implementation of <see cref="IProwlGraphicsInterop"/>.
/// Wraps Veldrid backend info APIs to provide stable native handle access.
/// </summary>
public sealed class ProwlGraphicsInterop : IProwlGraphicsInterop
{
    public GraphicsBackend BackendType
    {
        get
        {
            var device = Graphics.Device;
            return device?.BackendType ?? GraphicsBackend.Null;
        }
    }

    public event Action? DeviceCreated;
    public event Action? DeviceDestroying;

    public nint GetDeviceHandle()
    {
        var device = Graphics.Device;
        if (device == null) return 0;
        return device.BackendType switch
        {
#if !EXCLUDE_D3D11_BACKEND
            GraphicsBackend.Direct3D11 => device.GetD3D11Info().Device,
#endif
#if !EXCLUDE_VULKAN_BACKEND
            GraphicsBackend.Vulkan => device.GetVulkanInfo().Device,
#endif
            _ => 0,
        };
    }

    public nint GetAdapterHandle()
    {
        var device = Graphics.Device;
        if (device == null) return 0;
        return device.BackendType switch
        {
#if !EXCLUDE_D3D11_BACKEND
            GraphicsBackend.Direct3D11 => device.GetD3D11Info().Adapter,
#endif
#if !EXCLUDE_VULKAN_BACKEND
            GraphicsBackend.Vulkan => device.GetVulkanInfo().PhysicalDevice,
#endif
            _ => 0,
        };
    }

    public nint GetInstanceHandle()
    {
        var device = Graphics.Device;
        if (device == null) return 0;
#if !EXCLUDE_VULKAN_BACKEND
        if (device.BackendType == GraphicsBackend.Vulkan)
            return device.GetVulkanInfo().Instance;
#endif
        return 0;
    }

    public nint GetCommandQueueHandle()
    {
        var device = Graphics.Device;
        if (device == null) return 0;
#if !EXCLUDE_VULKAN_BACKEND
        if (device.BackendType == GraphicsBackend.Vulkan)
            return device.GetVulkanInfo().GraphicsQueue;
#endif
        return 0;
    }

    public uint GetQueueFamilyIndex()
    {
        var device = Graphics.Device;
        if (device == null) return 0;
#if !EXCLUDE_VULKAN_BACKEND
        if (device.BackendType == GraphicsBackend.Vulkan)
            return device.GetVulkanInfo().GraphicsQueueFamilyIndex;
#endif
        return 0;
    }

    public bool IsBackendSupported(GraphicsBackend backend)
    {
        return backend switch
        {
#if !EXCLUDE_D3D11_BACKEND
            GraphicsBackend.Direct3D11 => true,
#endif
#if !EXCLUDE_VULKAN_BACKEND
            GraphicsBackend.Vulkan => true,
#endif
            GraphicsBackend.OpenGL => true,
            GraphicsBackend.OpenGLES => true,
            _ => false,
        };
    }

    internal void RaiseDeviceCreated() => DeviceCreated?.Invoke();

    internal void RaiseDeviceDestroying() => DeviceDestroying?.Invoke();
}
