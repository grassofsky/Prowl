// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;

using Veldrid;
using Veldrid.StartupUtilities;
using Veldrid.Sdl2;

using Prowl.Runtime.Rendering.NativeRendering;

// Disable test parallelism to prevent races on static Graphics.Device/GraphicsInterop
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Prowl.Runtime.Test.GPU;

/// <summary>
/// Shared xUnit fixture that creates a headless GPU device for integration testing.
/// Priority: OpenGL (via hidden SDL2 window) → Vulkan (headless) → skip.
/// </summary>
public class GpuDeviceFixture : IDisposable
{
    public GraphicsDevice? Device { get; }
    public GraphicsBackend Backend => Device?.BackendType ?? GraphicsBackend.Null;
    public string? SkipReason { get; }

    private Sdl2Window? _hiddenWindow;

    public bool IsAvailable => Device != null;

    public GpuDeviceFixture()
    {
        var options = new GraphicsDeviceOptions
        {
            HasMainSwapchain = false,
            ResourceBindingModel = ResourceBindingModel.Default,
        };

        // 1) Try OpenGL via hidden SDL2 window
        GraphicsDevice? device = TryCreateOpenGL(options);

        // 2) Fallback to Vulkan headless
        if (device == null)
            device = TryCreateVulkan(options);

        if (device == null)
        {
            SkipReason = "No supported GPU backend (OpenGL/Vulkan) available on this machine.";
            return;
        }

        Device = device;

        // Wire up Graphics statics so production code paths work
        Graphics.Device = Device;
        var interop = new ProwlGraphicsInterop();
        Graphics.GraphicsInterop = interop;
        interop.RaiseDeviceCreated();
    }

    private GraphicsDevice? TryCreateOpenGL(GraphicsDeviceOptions options)
    {
        try
        {
            if (!GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL))
                return null;

            // OpenGL requires an SDL2 window + GL context
            var windowOptions = options with { HasMainSwapchain = true };
            _hiddenWindow = new Sdl2Window("GpuTest", 0, 0, 1, 1, SDL_WindowFlags.Hidden | SDL_WindowFlags.OpenGL, threadedProcessing: false);
            return VeldridStartup.CreateGraphicsDevice(_hiddenWindow, windowOptions, GraphicsBackend.OpenGL);
        }
        catch
        {
            _hiddenWindow?.Close();
            _hiddenWindow = null;
            return null;
        }
    }

    private static GraphicsDevice? TryCreateVulkan(GraphicsDeviceOptions options)
    {
        try
        {
            if (!GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
                return null;

            return GraphicsDevice.CreateVulkan(options);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (Graphics.GraphicsInterop is ProwlGraphicsInterop interop)
            interop.RaiseDeviceDestroying();

        Graphics.GraphicsInterop = null!;
        Graphics.Device = null!;

        Device?.Dispose();

        _hiddenWindow?.Close();
        _hiddenWindow = null;
    }
}

[CollectionDefinition("GPU")]
public class GpuTestCollection : ICollectionFixture<GpuDeviceFixture> { }
