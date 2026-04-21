// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;

using Veldrid;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test.GPU;

[Collection("GPU")]
[Trait("Category", "GPU")]
public class GpuTextureTests
{
    private readonly GpuDeviceFixture _gpu;

    public GpuTextureTests(GpuDeviceFixture fixture) => _gpu = fixture;

    [Fact]
    public void GetNativeTexturePtr_ValidTexture_ReturnsNonZero()
    {
        if (!_gpu.IsAvailable) return;

        var texture = new Texture2D(64, 64, format: PixelFormat.R8_G8_B8_A8_UNorm);
        nint ptr = texture.GetNativeTexturePtr();

        // All backends (OpenGL, Vulkan, D3D11) should return a valid handle
        Assert.NotEqual(0, (long)ptr);
        texture.OnDispose();
    }

    [Fact]
    public void GetNativeTexturePtr_ConsistentAcrossCalls()
    {
        if (!_gpu.IsAvailable) return;

        var texture = new Texture2D(32, 32, format: PixelFormat.R8_G8_B8_A8_UNorm);
        nint ptr1 = texture.GetNativeTexturePtr();
        nint ptr2 = texture.GetNativeTexturePtr();

        Assert.Equal(ptr1, ptr2);
        texture.OnDispose();
    }

    [Fact]
    public void GetNativeTexturePtr_DifferentTextures_DifferentHandles()
    {
        if (!_gpu.IsAvailable) return;

        var tex1 = new Texture2D(16, 16, format: PixelFormat.R8_G8_B8_A8_UNorm);
        var tex2 = new Texture2D(16, 16, format: PixelFormat.R8_G8_B8_A8_UNorm);

        nint ptr1 = tex1.GetNativeTexturePtr();
        nint ptr2 = tex2.GetNativeTexturePtr();

        Assert.NotEqual(ptr1, ptr2);
        tex1.OnDispose();
        tex2.OnDispose();
    }

    [Fact]
    public void CreateFromNativeHandle_Roundtrip()
    {
        if (!_gpu.IsAvailable) return;

        // Skip if backend is OpenGL — CreateTexture(nativeHandle) may not be supported
        if (_gpu.Backend == GraphicsBackend.OpenGL || _gpu.Backend == GraphicsBackend.OpenGLES)
            return;

        var original = new Texture2D(64, 64, format: PixelFormat.R8_G8_B8_A8_UNorm);
        nint nativePtr = original.GetNativeTexturePtr();
        Assert.NotEqual(0, (long)nativePtr);

        var description = new TextureDescription(
            64, 64, 1, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        // Wrap the native handle into a new Texture2D
        var wrapped = Texture2D.CreateFromNativeHandle((ulong)nativePtr, description);

        Assert.NotNull(wrapped);
        Assert.NotNull(wrapped.InternalTexture);
        Assert.Equal(64u, wrapped.Width);
        Assert.Equal(64u, wrapped.Height);

        wrapped.OnDispose();
        original.OnDispose();
    }

    [Fact]
    public void CreateFromNativeHandle_WrappedTexture_SameNativePtr()
    {
        if (!_gpu.IsAvailable) return;

        if (_gpu.Backend == GraphicsBackend.OpenGL || _gpu.Backend == GraphicsBackend.OpenGLES)
            return;

        var original = new Texture2D(32, 32, format: PixelFormat.R8_G8_B8_A8_UNorm);
        nint originalPtr = original.GetNativeTexturePtr();

        var description = new TextureDescription(
            32, 32, 1, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        var wrapped = Texture2D.CreateFromNativeHandle((ulong)originalPtr, description);
        nint wrappedPtr = wrapped.GetNativeTexturePtr();

        // Both textures should reference the same underlying GPU resource
        Assert.Equal(originalPtr, wrappedPtr);

        wrapped.OnDispose();
        original.OnDispose();
    }

    [Fact]
    public void GraphicsInterop_BackendMatches_DeviceBackend()
    {
        if (!_gpu.IsAvailable) return;

        Assert.Equal(_gpu.Device!.BackendType, Graphics.GraphicsInterop.BackendType);
    }

    [Fact]
    public void CreateFromNativeHandle_OwnsTexture_IsFalse()
    {
        if (!_gpu.IsAvailable) return;

        if (_gpu.Backend == GraphicsBackend.OpenGL || _gpu.Backend == GraphicsBackend.OpenGLES)
            return;

        var original = new Texture2D(32, 32, format: PixelFormat.R8_G8_B8_A8_UNorm);
        nint nativePtr = original.GetNativeTexturePtr();

        var description = new TextureDescription(
            32, 32, 1, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        var wrapped = Texture2D.CreateFromNativeHandle((ulong)nativePtr, description);

        // Wrapped texture does NOT own the native resource — caller is responsible
        Assert.False(wrapped.OwnsTexture);

        wrapped.OnDispose();
        original.OnDispose();
    }
}
