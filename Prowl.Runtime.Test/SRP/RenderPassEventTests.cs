// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class RenderPassEventTests
{
    [Fact]
    public void Values_AreInCorrectOrder()
    {
        Assert.True(RenderPassEvent.BeforeRendering < RenderPassEvent.BeforeRenderingShadows);
        Assert.True(RenderPassEvent.BeforeRenderingShadows < RenderPassEvent.AfterRenderingShadows);
        Assert.True(RenderPassEvent.AfterRenderingShadows < RenderPassEvent.BeforeRenderingPrepasses);
        Assert.True(RenderPassEvent.BeforeRenderingPrepasses < RenderPassEvent.AfterRenderingPrepasses);
        Assert.True(RenderPassEvent.AfterRenderingPrepasses < RenderPassEvent.BeforeRenderingOpaques);
        Assert.True(RenderPassEvent.BeforeRenderingOpaques < RenderPassEvent.OnRenderingOpaques);
        Assert.True(RenderPassEvent.OnRenderingOpaques < RenderPassEvent.AfterRenderingOpaques);
        Assert.True(RenderPassEvent.AfterRenderingOpaques < RenderPassEvent.AfterRenderingOpaquesPostProcess);
        Assert.True(RenderPassEvent.AfterRenderingOpaquesPostProcess < RenderPassEvent.BeforeRenderingSkybox);
        Assert.True(RenderPassEvent.BeforeRenderingSkybox < RenderPassEvent.OnRenderingSkybox);
        Assert.True(RenderPassEvent.OnRenderingSkybox < RenderPassEvent.AfterRenderingSkybox);
        Assert.True(RenderPassEvent.AfterRenderingSkybox < RenderPassEvent.BeforeRenderingTransparents);
        Assert.True(RenderPassEvent.BeforeRenderingTransparents < RenderPassEvent.OnRenderingTransparents);
        Assert.True(RenderPassEvent.OnRenderingTransparents < RenderPassEvent.AfterRenderingTransparents);
        Assert.True(RenderPassEvent.AfterRenderingTransparents < RenderPassEvent.BeforeRenderingPostProcessing);
        Assert.True(RenderPassEvent.BeforeRenderingPostProcessing < RenderPassEvent.AfterRenderingPostProcessing);
        Assert.True(RenderPassEvent.AfterRenderingPostProcessing < RenderPassEvent.AfterRendering);
    }

    [Fact]
    public void BeforeRendering_IsZero()
    {
        Assert.Equal(0, (int)RenderPassEvent.BeforeRendering);
    }

    [Fact]
    public void AfterRendering_IsLargest()
    {
        Assert.Equal(1000, (int)RenderPassEvent.AfterRendering);
    }

    [Theory]
    [InlineData(RenderPassEvent.BeforeRendering, 0)]
    [InlineData(RenderPassEvent.BeforeRenderingShadows, 50)]
    [InlineData(RenderPassEvent.AfterRenderingShadows, 100)]
    [InlineData(RenderPassEvent.BeforeRenderingPrepasses, 150)]
    [InlineData(RenderPassEvent.AfterRenderingPrepasses, 200)]
    [InlineData(RenderPassEvent.BeforeRenderingOpaques, 250)]
    [InlineData(RenderPassEvent.OnRenderingOpaques, 275)]
    [InlineData(RenderPassEvent.AfterRenderingOpaques, 300)]
    [InlineData(RenderPassEvent.AfterRenderingOpaquesPostProcess, 310)]
    [InlineData(RenderPassEvent.BeforeRenderingSkybox, 350)]
    [InlineData(RenderPassEvent.OnRenderingSkybox, 375)]
    [InlineData(RenderPassEvent.AfterRenderingSkybox, 400)]
    [InlineData(RenderPassEvent.BeforeRenderingTransparents, 450)]
    [InlineData(RenderPassEvent.OnRenderingTransparents, 475)]
    [InlineData(RenderPassEvent.AfterRenderingTransparents, 500)]
    [InlineData(RenderPassEvent.BeforeRenderingPostProcessing, 550)]
    [InlineData(RenderPassEvent.AfterRenderingPostProcessing, 600)]
    [InlineData(RenderPassEvent.AfterRendering, 1000)]
    public void EventValues_AreCorrect(RenderPassEvent evt, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)evt);
    }
}
