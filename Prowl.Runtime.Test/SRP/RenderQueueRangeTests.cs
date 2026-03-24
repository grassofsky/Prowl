// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class RenderQueueRangeTests
{
    [Fact]
    public void Opaque_ContainsCorrectRange()
    {
        var range = RenderQueueRange.Opaque;
        
        Assert.Equal(0, range.LowerBound);
        Assert.Equal(2500, range.UpperBound);
    }

    [Fact]
    public void Transparent_ContainsCorrectRange()
    {
        var range = RenderQueueRange.Transparent;
        
        Assert.Equal(2501, range.LowerBound);
        Assert.Equal(5000, range.UpperBound);
    }

    [Fact]
    public void All_ContainsFullRange()
    {
        var range = RenderQueueRange.All;
        
        Assert.Equal(0, range.LowerBound);
        Assert.Equal(5000, range.UpperBound);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1000, true)]
    [InlineData(2500, true)]
    [InlineData(2501, false)]
    [InlineData(3000, false)]
    public void Opaque_ContainsCorrectValues(int renderQueue, bool expected)
    {
        Assert.Equal(expected, RenderQueueRange.Opaque.Contains(renderQueue));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2500, false)]
    [InlineData(2501, true)]
    [InlineData(3000, true)]
    [InlineData(5000, true)]
    public void Transparent_ContainsCorrectValues(int renderQueue, bool expected)
    {
        Assert.Equal(expected, RenderQueueRange.Transparent.Contains(renderQueue));
    }

    [Fact]
    public void CustomRange_WorksCorrectly()
    {
        var range = new RenderQueueRange(100, 200);
        
        Assert.False(range.Contains(50));
        Assert.True(range.Contains(100));
        Assert.True(range.Contains(150));
        Assert.True(range.Contains(200));
        Assert.False(range.Contains(250));
    }
}
