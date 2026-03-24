// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class DrawingSettingsTests
{
    [Fact]
    public void Constructor_WithSingleTag_SetsShaderPassName()
    {
        var settings = new DrawingSettings(
            new ShaderTagId("Opaque"),
            new SortingSettings((Camera?)null));
        
        Assert.Single(settings.ShaderPassNames);
        Assert.Equal("Opaque", settings.ShaderPassNames[0].Name);
    }

    [Fact]
    public void Constructor_WithMultipleTags_SetsShaderPassNames()
    {
        var tags = new[] { new ShaderTagId("Opaque"), new ShaderTagId("Forward") };
        var settings = new DrawingSettings(tags, new SortingSettings((Camera?)null));
        
        Assert.Equal(2, settings.ShaderPassNames.Length);
    }

    [Fact]
    public void SetOverrideMaterial_SetsMaterial()
    {
        var settings = new DrawingSettings(
            new ShaderTagId("Opaque"),
            new SortingSettings((Camera?)null));
        
        settings.SetOverrideMaterial(null);
        
        Assert.Null(settings.OverrideMaterial);
    }

    [Fact]
    public void SetPerObjectData_SetsValue()
    {
        var settings = new DrawingSettings(
            new ShaderTagId("Opaque"),
            new SortingSettings((Camera?)null));
        
        settings.SetPerObjectData(PerObjectData.MotionVectors | PerObjectData.LightProbe);
        
        Assert.Equal(PerObjectData.MotionVectors | PerObjectData.LightProbe, settings.PerObjectData);
    }
}

public class FilteringSettingsTests
{
    [Fact]
    public void Constructor_WithRenderQueueRange_SetsRange()
    {
        var settings = new FilteringSettings(RenderQueueRange.Opaque);
        
        Assert.Equal(RenderQueueRange.Opaque.LowerBound, settings.RenderQueueRange.LowerBound);
        Assert.Equal(RenderQueueRange.Opaque.UpperBound, settings.RenderQueueRange.UpperBound);
    }

    [Fact]
    public void Constructor_WithLayerMask_SetsLayerMask()
    {
        var layerMask = LayerMask.Everything;
        var settings = new FilteringSettings(RenderQueueRange.All, layerMask);
        
        Assert.Equal(layerMask, settings.LayerMask);
    }

    [Fact]
    public void Default_HasEverythingLayerMask()
    {
        var settings = new FilteringSettings(RenderQueueRange.All);
        
        Assert.Equal(LayerMask.Everything, settings.LayerMask);
    }

    [Fact]
    public void SetLayerMask_UpdatesValue()
    {
        var settings = new FilteringSettings(RenderQueueRange.All);
        var newMask = LayerMask.Nothing;
        
        settings.SetLayerMask(newMask);
        
        Assert.Equal(newMask, settings.LayerMask);
    }
}

public class SortingSettingsTests
{
    [Fact]
    public void Constructor_WithNullCamera_SetsDefaultCriteria()
    {
        var settings = new SortingSettings((Camera?)null);
        
        Assert.Equal(SortingCriteria.Default, settings.Criteria);
    }

    [Fact]
    public void Constructor_WithCriteria_SetsCriteria()
    {
        var settings = new SortingSettings((Camera?)null, SortingCriteria.BackToFront);
        
        Assert.Equal(SortingCriteria.BackToFront, settings.Criteria);
    }
}

public class SortingCriteriaTests
{
    [Fact]
    public void Default_IsCombinationOfFlags()
    {
        var expected = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges;
        
        Assert.Equal(expected, SortingCriteria.Default);
    }

    [Fact]
    public void None_IsZero()
    {
        Assert.Equal(0, (int)SortingCriteria.None);
    }
}

public class PerObjectDataTests
{
    [Fact]
    public void None_IsZero()
    {
        Assert.Equal(0, (int)PerObjectData.None);
    }

    [Fact]
    public void CanCombineFlags()
    {
        var combined = PerObjectData.MotionVectors | PerObjectData.LightIndices | PerObjectData.LightProbe;
        
        Assert.True(combined.HasFlag(PerObjectData.MotionVectors));
        Assert.True(combined.HasFlag(PerObjectData.LightIndices));
        Assert.True(combined.HasFlag(PerObjectData.LightProbe));
    }
}
