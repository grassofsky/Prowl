// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;

using Veldrid;
using Prowl.Runtime.Rendering.NativeRendering;

namespace Prowl.Runtime.Test.GPU;

[Collection("GPU")]
[Trait("Category", "GPU")]
public class GpuInteropTests
{
    private readonly GpuDeviceFixture _gpu;

    public GpuInteropTests(GpuDeviceFixture fixture) => _gpu = fixture;

    [Fact]
    public void GraphicsInterop_NotNull_AfterFixture()
    {
        if (!_gpu.IsAvailable) return;

        Assert.NotNull(Graphics.GraphicsInterop);
    }

    [Fact]
    public void BackendType_MatchesDevice()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        Assert.Equal(_gpu.Device!.BackendType, interop.BackendType);
    }

    [Fact]
    public void GetDeviceHandle_ReturnsNonZero()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        nint handle = interop.GetDeviceHandle();

        // OpenGL doesn't expose a device pointer — only D3D11/Vulkan return non-zero
        if (_gpu.Backend == GraphicsBackend.Direct3D11 || _gpu.Backend == GraphicsBackend.Vulkan)
            Assert.NotEqual(0, (long)handle);
    }

    [Fact]
    public void GetAdapterHandle_ReturnsNonZero()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        nint handle = interop.GetAdapterHandle();

        if (_gpu.Backend == GraphicsBackend.Direct3D11 || _gpu.Backend == GraphicsBackend.Vulkan)
            Assert.NotEqual(0, (long)handle);
    }

    [Fact]
    public void GetInstanceHandle_Vulkan_NonZero_Others_Zero()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        nint handle = interop.GetInstanceHandle();

        if (_gpu.Backend == GraphicsBackend.Vulkan)
            Assert.NotEqual(0, (long)handle);
        else
            Assert.Equal(0, (long)handle);
    }

    [Fact]
    public void GetCommandQueueHandle_Vulkan_NonZero_Others_Zero()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        nint handle = interop.GetCommandQueueHandle();

        if (_gpu.Backend == GraphicsBackend.Vulkan)
            Assert.NotEqual(0, (long)handle);
        else
            Assert.Equal(0, (long)handle);
    }

    [Fact]
    public void IsBackendSupported_CurrentBackend_ReturnsTrue()
    {
        if (!_gpu.IsAvailable) return;

        var interop = (ProwlGraphicsInterop)Graphics.GraphicsInterop;
        Assert.True(interop.IsBackendSupported(_gpu.Backend));
    }
}
