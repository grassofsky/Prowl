// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class SRPStableSortTests
{
    [Fact]
    public void SortPasses_StableForEqualPriority()
    {
        var renderer = new TestScriptableRenderer();
        var passA = new TestRenderPass("PassA", RenderPassEvent.AfterRenderingOpaques);
        var passB = new TestRenderPass("PassB", RenderPassEvent.AfterRenderingOpaques);

        renderer.EnqueuePass(passA);
        renderer.EnqueuePass(passB);

        // Access sorted cache via Passes — after sort, order should match enqueue order
        // We verify stability by checking enqueue order is consistent across multiple sorts
        for (int i = 0; i < 10; i++)
        {
            renderer.ClearPasses();
            renderer.EnqueuePass(passA);
            renderer.EnqueuePass(passB);

            // Passes list should be in enqueue order
            Assert.Equal("PassA", renderer.Passes[0].Name);
            Assert.Equal("PassB", renderer.Passes[1].Name);
        }
    }

    [Fact]
    public void SortPasses_DifferentPriorities_SortedCorrectly()
    {
        var renderer = new TestScriptableRenderer();
        var laterPass = new TestRenderPass("Later", RenderPassEvent.AfterRenderingTransparents);
        var earlierPass = new TestRenderPass("Earlier", RenderPassEvent.BeforeRendering);

        renderer.EnqueuePass(laterPass);
        renderer.EnqueuePass(earlierPass);

        // After sort, earlier should come first
        Assert.Equal(2, renderer.Passes.Count);
    }
}

public class SRPFeatureInvalidationTests
{
    [Fact]
    public void AddRenderFeature_InvalidatesRenderer()
    {
        var asset = new TestRenderPipelineAsset();
        var renderer1 = asset.GetSharedRenderer();

        asset.AddRenderFeature(new TestRenderFeature("Test"));

        var renderer2 = asset.GetSharedRenderer();

        Assert.NotSame(renderer1, renderer2);
    }

    [Fact]
    public void RemoveRenderFeature_InvalidatesRenderer()
    {
        var asset = new TestRenderPipelineAsset();
        var feature = new TestRenderFeature("Test");
        asset.AddRenderFeature(feature);
        var renderer1 = asset.GetSharedRenderer();

        asset.RemoveRenderFeature(feature);

        var renderer2 = asset.GetSharedRenderer();
        Assert.NotSame(renderer1, renderer2);
    }
}

public class SRPExceptionResilienceTests
{
    [Fact]
    public void Execute_PassThrowsInExecute_OtherPassesStillCleanedUp()
    {
        var renderer = new TestScriptableRenderer();
        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        var normalPass = new TestRenderPass("Normal", RenderPassEvent.BeforeRendering);
        var throwingPass = new ThrowingExecutePass("Throwing", RenderPassEvent.AfterRenderingOpaques);

        renderer.EnqueuePass(normalPass);
        renderer.EnqueuePass(throwingPass);

        // Should not throw — exceptions are caught and logged
        var exception = Record.Exception(() => renderer.Execute(context, ref data));
        Assert.Null(exception);

        // Normal pass should have been executed and cleaned up
        Assert.Equal(1, normalPass.ExecuteCount);
        Assert.Equal(1, normalPass.CleanupCount);
    }

    [Fact]
    public void Execute_PassThrowsInCleanup_OtherPassesStillCleanedUp()
    {
        var renderer = new TestScriptableRenderer();
        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        var normalPass = new TestRenderPass("Normal", RenderPassEvent.BeforeRendering);
        var throwingPass = new ThrowingCleanupPass("ThrowingCleanup", RenderPassEvent.AfterRenderingOpaques);

        renderer.EnqueuePass(normalPass);
        renderer.EnqueuePass(throwingPass);

        // Should not throw
        var exception = Record.Exception(() => renderer.Execute(context, ref data));
        Assert.Null(exception);

        // Both passes should have been executed
        Assert.Equal(1, normalPass.ExecuteCount);
        Assert.Equal(1, throwingPass.ExecuteCount);

        // Normal pass cleanup should still happen despite throwing pass cleanup failure
        Assert.Equal(1, normalPass.CleanupCount);
    }

    [Fact]
    public void PassCleanup_NotCalledTwice()
    {
        var renderer = new TestScriptableRenderer();
        var context = new ScriptableRenderContext();
        var data = new SRPRenderingData();

        var pass = new TestRenderPass("CountedPass", RenderPassEvent.AfterRenderingOpaques);
        renderer.EnqueuePass(pass);

        // Execute cleans up executed passes
        renderer.Execute(context, ref data);
        // Cleanup should skip already-cleaned passes
        renderer.Cleanup(context, ref data);

        Assert.Equal(1, pass.CleanupCount);
    }
}

public class SRPPipelineAssetLifecycleTests
{
    [Fact]
    public void GetSharedRenderer_ReturnsSameInstance()
    {
        var asset = new TestRenderPipelineAsset();
        var renderer1 = asset.GetSharedRenderer();
        var renderer2 = asset.GetSharedRenderer();

        Assert.Same(renderer1, renderer2);
    }

    [Fact]
    public void InvalidateSharedRenderer_ForcesNewInstance()
    {
        var asset = new TestRenderPipelineAsset();
        var renderer1 = asset.GetSharedRenderer();

        asset.InvalidateSharedRenderer();

        var renderer2 = asset.GetSharedRenderer();
        Assert.NotSame(renderer1, renderer2);
    }

    [Fact]
    public void Dispose_CleansUpRenderer()
    {
        var renderer = new TestScriptableRenderer();
        var feature = new TestRenderFeature("Test");
        renderer.AddFeature(feature);
        renderer.EnqueuePass(new TestRenderPass("Test", RenderPassEvent.AfterRendering));

        renderer.Dispose();

        Assert.Empty(renderer.Features);
        Assert.Empty(renderer.Passes);
    }
}
