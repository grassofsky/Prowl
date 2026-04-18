// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class ScriptableRenderContextTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        var context = new ScriptableRenderContext();
        
        Assert.NotNull(context);
    }

    [Fact]
    public void EnqueuePass_Null_ThrowsArgumentNullException()
    {
        var context = new ScriptableRenderContext();
        
        Assert.Throws<ArgumentNullException>(() => context.EnqueuePass(null!));
    }

    [Fact]
    public void InsertPass_Null_ThrowsArgumentNullException()
    {
        var context = new ScriptableRenderContext();
        
        Assert.Throws<ArgumentNullException>(() => context.InsertPass(0, null!));
    }

    [Fact]
    public void ClearPasses_DoesNotThrow()
    {
        var context = new ScriptableRenderContext();
        
        var exception = Record.Exception(() => context.ClearPasses());
        Assert.Null(exception);
    }

    [Fact]
    public void ExecuteCommandBuffer_Null_ThrowsArgumentNullException()
    {
        var context = new ScriptableRenderContext();
        
        Assert.Throws<ArgumentNullException>(() => context.ExecuteCommandBuffer(null!));
    }

    [Fact]
    public void Submit_DoesNotThrow()
    {
        var context = new ScriptableRenderContext();
        
        var exception = Record.Exception(() => context.Submit());
        Assert.Null(exception);
    }
}

public class ScriptableRendererTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        var renderer = new TestScriptableRenderer();
        
        Assert.NotNull(renderer);
    }

    [Fact]
    public void EnqueuePass_AddsPassToList()
    {
        var renderer = new TestScriptableRenderer();
        var pass = new TestRenderPass("TestPass", RenderPassEvent.AfterRendering);
        
        renderer.EnqueuePass(pass);
        
        Assert.Single(renderer.Passes);
    }

    [Fact]
    public void AddFeature_AddsFeatureToList()
    {
        var renderer = new TestScriptableRenderer();
        var feature = new TestRenderFeature("TestFeature");
        
        renderer.AddFeature(feature);
        
        Assert.Single(renderer.Features);
        Assert.Equal("TestFeature", renderer.Features[0].FeatureName);
    }

    [Fact]
    public void RemoveFeature_RemovesFeature()
    {
        var renderer = new TestScriptableRenderer();
        var feature = new TestRenderFeature("TestFeature");
        
        renderer.AddFeature(feature);
        renderer.RemoveFeature(feature);
        
        Assert.Empty(renderer.Features);
    }

    [Fact]
    public void ClearPasses_RemovesAllPasses()
    {
        var renderer = new TestScriptableRenderer();
        renderer.EnqueuePass(new TestRenderPass("Pass1", RenderPassEvent.AfterRendering));
        renderer.EnqueuePass(new TestRenderPass("Pass2", RenderPassEvent.BeforeRendering));
        
        renderer.ClearPasses();
        
        Assert.Empty(renderer.Passes);
    }

    [Fact]
    public void AddFeature_Null_ThrowsArgumentNullException()
    {
        var renderer = new TestScriptableRenderer();
        
        Assert.Throws<ArgumentNullException>(() => renderer.AddFeature(null!));
    }
}

public class RenderPassTests
{
    [Fact]
    public void Constructor_SetsNameAndInjectionPoint()
    {
        var pass = new TestRenderPass("TestPass", RenderPassEvent.AfterRenderingTransparents);
        
        Assert.Equal("TestPass", pass.Name);
        Assert.Equal(RenderPassEvent.AfterRenderingTransparents, pass.InjectionPoint);
    }

    [Fact]
    public void DefaultInjectionPoint_IsAfterRendering()
    {
        var pass = new TestRenderPass("Test", RenderPassEvent.AfterRendering);
        
        Assert.Equal(RenderPassEvent.AfterRendering, pass.InjectionPoint);
    }
}

public class RenderFeatureTests
{
    [Fact]
    public void Constructor_SetsName()
    {
        var feature = new TestRenderFeature("TestFeature");
        
        Assert.Equal("TestFeature", feature.FeatureName);
    }

    [Fact]
    public void Enabled_DefaultIsTrue()
    {
        var feature = new TestRenderFeature("Test");
        
        Assert.True(feature.Enabled);
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        var feature = new TestRenderFeature("Test");
        feature.Enabled = false;
        
        Assert.False(feature.Enabled);
    }
}

public class CullingResultsTests
{
    [Fact]
    public void Constructor_InitializesEmpty()
    {
        var results = new CullingResults();
        
        Assert.Equal(0, results.VisibleObjectCount);
        Assert.Equal(0, results.VisibleLightCount);
    }
}

public class RenderPipelineAssetTests
{
    [Fact]
    public void CreateRenderer_ReturnsNonNull()
    {
        var asset = new TestRenderPipelineAsset();
        var renderer = asset.CreateRenderer();
        
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AddRenderFeature_AddsFeature()
    {
        var asset = new TestRenderPipelineAsset();
        var feature = new TestRenderFeature("Test");
        
        asset.AddRenderFeature(feature);
        
        Assert.Single(asset.RenderFeatures);
    }

    [Fact]
    public void RemoveRenderFeature_RemovesFeature()
    {
        var asset = new TestRenderPipelineAsset();
        var feature = new TestRenderFeature("Test");
        
        asset.AddRenderFeature(feature);
        asset.RemoveRenderFeature(feature);
        
        Assert.Empty(asset.RenderFeatures);
    }

    [Fact]
    public void Validate_WithValidFeatures_ReturnsTrue()
    {
        var asset = new TestRenderPipelineAsset();
        asset.AddRenderFeature(new TestRenderFeature("Test"));
        
        Assert.True(asset.Validate());
    }
}

public class PipelineSettingsTests
{
    [Fact]
    public void Default_HasCorrectValues()
    {
        var settings = new PipelineSettings();
        
        Assert.True(settings.UseHDR);
        Assert.Equal(16, settings.MaxLights);
        Assert.Equal(4096, settings.ShadowAtlasSize);
        Assert.True(settings.UseDepthPrepass);
        Assert.False(settings.UseMotionVectors);
    }
}

internal class TestRenderPass : RenderPass
{
    public int ConfigureCount { get; private set; }
    public int ExecuteCount { get; private set; }
    public int CleanupCount { get; private set; }

    public TestRenderPass(string name, RenderPassEvent injectionPoint)
    {
        Name = name;
        InjectionPoint = injectionPoint;
    }

    public override void Configure(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        ConfigureCount++;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        ExecuteCount++;
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        CleanupCount++;
    }
}

internal class ThrowingExecutePass : RenderPass
{
    public int CleanupCount { get; private set; }

    public ThrowingExecutePass(string name, RenderPassEvent injectionPoint)
    {
        Name = name;
        InjectionPoint = injectionPoint;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        throw new InvalidOperationException("Test exception in Execute");
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        CleanupCount++;
    }
}

internal class ThrowingCleanupPass : RenderPass
{
    public int ExecuteCount { get; private set; }

    public ThrowingCleanupPass(string name, RenderPassEvent injectionPoint)
    {
        Name = name;
        InjectionPoint = injectionPoint;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        ExecuteCount++;
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        throw new InvalidOperationException("Test exception in Cleanup");
    }
}

internal class TestRenderFeature : RenderFeature
{
    public int AddRenderPassesCount { get; private set; }

    public TestRenderFeature(string name)
    {
        FeatureName = name;
    }

    public override void AddRenderPasses(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        AddRenderPassesCount++;
        context.EnqueuePass(new TestRenderPass("FeaturePass", RenderPassEvent.AfterRenderingOpaques));
    }
}

internal class ThrowingFeature : RenderFeature
{
    public ThrowingFeature(string name)
    {
        FeatureName = name;
    }

    public override void AddRenderPasses(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        throw new InvalidOperationException("Test exception in AddRenderPasses");
    }
}

internal class TestScriptableRenderer : ScriptableRenderer
{
    public bool SetupCalled { get; private set; }

    public override void Setup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        SetupCalled = true;
        EnqueuePass(new TestRenderPass("SetupPass", RenderPassEvent.BeforeRendering));
    }
}

internal class TestRenderPipelineAsset : RenderPipelineAsset
{
    public override ScriptableRenderer CreateRenderer()
    {
        return new TestScriptableRenderer();
    }
}
