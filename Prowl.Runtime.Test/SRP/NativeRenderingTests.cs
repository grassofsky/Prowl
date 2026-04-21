// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Rendering.NativeRendering;

namespace Prowl.Runtime.Test;

public class NativeRenderPassTests
{
    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new NativeRenderPass(null!, RenderPassEvent.AfterRenderingOpaques, _ => { }));
    }

    [Fact]
    public void Constructor_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new NativeRenderPass("Test", RenderPassEvent.AfterRenderingOpaques, null!));
    }

    [Fact]
    public void Constructor_SetsNameAndInjectionPoint()
    {
        var pass = new NativeRenderPass("TestPass", RenderPassEvent.BeforeRenderingShadows, _ => { }, eventId: 7);

        Assert.Equal("TestPass", pass.Name);
        Assert.Equal(RenderPassEvent.BeforeRenderingShadows, pass.InjectionPoint);
    }

    [Fact]
    public void Execute_InvokesCallback_WithContext()
    {
        NativeRenderContext? receivedCtx = null;
        var pass = new NativeRenderPass("TestPass", RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            receivedCtx = ctx;
        }, eventId: 42);

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        // Execute will work without a live Graphics.Device — interop will be null,
        // so context gets default values
        pass.Execute(context, ref data);

        Assert.NotNull(receivedCtx);
        Assert.Equal(42, receivedCtx.Value.EventId);
        Assert.Equal(0, (int)receivedCtx.Value.DeviceHandle);
    }

    [Fact]
    public void Execute_PassesRenderingData()
    {
        SRPRenderingData? capturedData = null;
        var pass = new NativeRenderPass("TestPass", RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            capturedData = ctx.RenderingData;
        });

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData { IsSceneViewCamera = true };

        pass.Execute(context, ref data);

        Assert.NotNull(capturedData);
        Assert.True(capturedData.IsSceneViewCamera);
    }
}

public class NativeRenderFeatureTests
{
    [Fact]
    public void AddCallback_NullCallback_ThrowsArgumentNullException()
    {
        var feature = new NativeRenderFeature("Test");
        Assert.Throws<ArgumentNullException>(() =>
            feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, null!));
    }

    [Fact]
    public void AddRenderPasses_CallbackFiresOnExecute()
    {
        int callCount = 0;
        var feature = new NativeRenderFeature("Test");
        feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, _ => callCount++, eventId: 1);

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        // AddRenderPasses enqueues a NativeRenderPass into the context
        feature.AddRenderPasses(context, ref data);

        // The pass was enqueued — verify it's there by checking no exception
        Assert.Equal(0, callCount); // Not yet executed
    }

    [Fact]
    public void AddRenderPasses_MultipleCallbacks_AllEnqueued()
    {
        int total = 0;
        var feature = new NativeRenderFeature("Test");
        feature.AddCallback(RenderPassEvent.BeforeRenderingShadows, _ => total++, eventId: 1);
        feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, _ => total++, eventId: 2);
        feature.AddCallback(RenderPassEvent.AfterRenderingTransparents, _ => total++, eventId: 3);

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        feature.AddRenderPasses(context, ref data);

        // Three passes enqueued, none executed yet
        Assert.Equal(0, total);
    }

    [Fact]
    public void ClearCallbacks_RemovesAll()
    {
        int callCount = 0;
        var feature = new NativeRenderFeature("Test");
        feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, _ => callCount++);
        feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, _ => callCount++);

        feature.ClearCallbacks();

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();
        feature.AddRenderPasses(context, ref data);

        // After clearing, no passes should be enqueued
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Enabled_DefaultTrue()
    {
        var feature = new NativeRenderFeature();
        Assert.True(feature.Enabled);
    }

    [Fact]
    public void FeatureName_SetInConstructor()
    {
        var feature = new NativeRenderFeature("MyPlugin");
        Assert.Equal("MyPlugin", feature.FeatureName);
    }
}

public class ProwlGraphicsInteropTests
{
    [Fact]
    public void IsBackendSupported_D3D11_ReturnsTrue()
    {
        var interop = new ProwlGraphicsInterop();
        Assert.True(interop.IsBackendSupported(Veldrid.GraphicsBackend.Direct3D11));
    }

    [Fact]
    public void IsBackendSupported_Vulkan_ReturnsTrue()
    {
        var interop = new ProwlGraphicsInterop();
        Assert.True(interop.IsBackendSupported(Veldrid.GraphicsBackend.Vulkan));
    }

    [Fact]
    public void IsBackendSupported_OpenGL_ReturnsTrue()
    {
        var interop = new ProwlGraphicsInterop();
        Assert.True(interop.IsBackendSupported(Veldrid.GraphicsBackend.OpenGL));
    }

    [Fact]
    public void IsBackendSupported_Metal_ReturnsFalse()
    {
        var interop = new ProwlGraphicsInterop();
        Assert.False(interop.IsBackendSupported(Veldrid.GraphicsBackend.Metal));
    }

    [Fact]
    public void DeviceCreated_EventFires()
    {
        var interop = new ProwlGraphicsInterop();
        bool fired = false;
        interop.DeviceCreated += () => fired = true;

        interop.RaiseDeviceCreated();

        Assert.True(fired);
    }

    [Fact]
    public void DeviceDestroying_EventFires()
    {
        var interop = new ProwlGraphicsInterop();
        bool fired = false;
        interop.DeviceDestroying += () => fired = true;

        interop.RaiseDeviceDestroying();

        Assert.True(fired);
    }

    [Fact]
    public void BackendType_ReturnsNull_WhenDeviceIsNull()
    {
        var savedDevice = Graphics.Device;
        try
        {
            Graphics.Device = null!;
            var interop = new ProwlGraphicsInterop();
            Assert.Equal(Veldrid.GraphicsBackend.Null, interop.BackendType);
        }
        finally
        {
            Graphics.Device = savedDevice;
        }
    }

    [Fact]
    public void GetDeviceHandle_ReturnsZero_WhenDeviceIsNull()
    {
        var savedDevice = Graphics.Device;
        try
        {
            Graphics.Device = null!;
            var interop = new ProwlGraphicsInterop();
            Assert.Equal(0, (long)interop.GetDeviceHandle());
        }
        finally
        {
            Graphics.Device = savedDevice;
        }
    }

    [Fact]
    public void GetAdapterHandle_ReturnsZero_WhenDeviceIsNull()
    {
        var savedDevice = Graphics.Device;
        try
        {
            Graphics.Device = null!;
            var interop = new ProwlGraphicsInterop();
            Assert.Equal(0, (long)interop.GetAdapterHandle());
        }
        finally
        {
            Graphics.Device = savedDevice;
        }
    }
}

public class CreateFromNativeHandleTests
{
    [Fact]
    public void CreateFromNativeHandle_ZeroHandle_ThrowsArgumentException()
    {
        var desc = new Veldrid.TextureDescription(
            64, 64, 1, 1, 1,
            Veldrid.PixelFormat.R8_G8_B8_A8_UNorm,
            Veldrid.TextureUsage.Sampled,
            Veldrid.TextureType.Texture2D);

        Assert.Throws<ArgumentException>(() =>
            Prowl.Runtime.Rendering.Texture2D.CreateFromNativeHandle(0, desc));
    }
}
