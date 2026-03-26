// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class ClearFlagTests
{
    [Fact]
    public void Default_IsNone()
    {
        var flag = ClearFlag.None;
        
        Assert.Equal(0, (int)flag);
    }

    [Theory]
    [InlineData(ClearFlag.Color, 1)]
    [InlineData(ClearFlag.Depth, 2)]
    [InlineData(ClearFlag.Stencil, 4)]
    [InlineData(ClearFlag.All, 7)]
    public void FlagValues_AreCorrect(ClearFlag flag, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)flag);
    }

    [Fact]
    public void All_IsCombinationOfColorDepthStencil()
    {
        var expected = ClearFlag.Color | ClearFlag.Depth | ClearFlag.Stencil;
        
        Assert.Equal(expected, ClearFlag.All);
    }

    [Theory]
    [InlineData(ClearFlag.Color, ClearFlag.Color, true)]
    [InlineData(ClearFlag.Color | ClearFlag.Depth, ClearFlag.Color, true)]
    [InlineData(ClearFlag.Depth, ClearFlag.Color, false)]
    public void HasFlag_WorksCorrectly(ClearFlag value, ClearFlag flag, bool expected)
    {
        Assert.Equal(expected, value.HasFlag(flag));
    }
}
