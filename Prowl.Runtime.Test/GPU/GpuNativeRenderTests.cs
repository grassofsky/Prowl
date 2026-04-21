// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;

using Veldrid;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Rendering.NativeRendering;

namespace Prowl.Runtime.Test.GPU;

[Collection("GPU")]
[Trait("Category", "GPU")]
public class GpuNativeRenderTests
{
    private readonly GpuDeviceFixture _gpu;

    public GpuNativeRenderTests(GpuDeviceFixture fixture) => _gpu = fixture;

    [Fact]
    public void NativeRenderPass_Execute_DeviceHandleNonZero()
    {
        if (!_gpu.IsAvailable) return;

        // OpenGL doesn't have a device pointer — only check D3D11/Vulkan
        if (_gpu.Backend != GraphicsBackend.Direct3D11 && _gpu.Backend != GraphicsBackend.Vulkan)
            return;

        nint capturedHandle = 0;
        var pass = new NativeRenderPass("GpuTest", RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            capturedHandle = ctx.DeviceHandle;
        }, eventId: 100);

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        pass.Execute(context, ref data);

        Assert.NotEqual(0, (long)capturedHandle);
    }

    [Fact]
    public void NativeRenderPass_Execute_BackendTypeCorrect()
    {
        if (!_gpu.IsAvailable) return;

        GraphicsBackend capturedBackend = default;
        var pass = new NativeRenderPass("GpuTest", RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            capturedBackend = ctx.BackendType;
        });

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        pass.Execute(context, ref data);

        Assert.Equal(_gpu.Device!.BackendType, capturedBackend);
    }

    [Fact]
    public void NativeRenderFeature_FullPipeline_CallbackReceivesValidContext()
    {
        if (!_gpu.IsAvailable) return;

        NativeRenderContext? captured = null;
        var feature = new NativeRenderFeature("GpuTestFeature");
        feature.AddCallback(RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            captured = ctx;
        }, eventId: 77);

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData { IsSceneViewCamera = true };

        // Enqueue the pass from the feature
        feature.AddRenderPasses(context, ref data);

        // Execute enqueued passes via internal API (now accessible via InternalsVisibleTo)
        var passes = context.GetPasses();
        Assert.Single(passes);

        // Execute the pass directly
        passes[0].Execute(context, ref data);

        Assert.NotNull(captured);
        Assert.Equal(77, captured.Value.EventId);
        Assert.Equal(_gpu.Device!.BackendType, captured.Value.BackendType);
        Assert.NotNull(captured.Value.RenderingData);
        Assert.True(captured.Value.RenderingData.IsSceneViewCamera);
    }

    [Fact]
    public void NativeRenderPass_Execute_WithOpenGL_DeviceHandleIsZero()
    {
        if (!_gpu.IsAvailable) return;

        // This test specifically validates OpenGL path where device handle is expected to be 0
        if (_gpu.Backend != GraphicsBackend.OpenGL && _gpu.Backend != GraphicsBackend.OpenGLES)
            return;

        nint capturedHandle = -1;
        var pass = new NativeRenderPass("GpuTest", RenderPassEvent.AfterRenderingOpaques, ctx =>
        {
            capturedHandle = ctx.DeviceHandle;
        });

        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        pass.Execute(context, ref data);

        // OpenGL doesn't expose a device pointer through Veldrid BackendInfo
        Assert.Equal(0, (long)capturedHandle);
    }
}
