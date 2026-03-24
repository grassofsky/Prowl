// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Xunit;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Test;

public class ShaderTagIdTests
{
    [Fact]
    public void Constructor_SetsName()
    {
        var tag = new ShaderTagId("Opaque");
        
        Assert.Equal("Opaque", tag.Name);
    }

    [Fact]
    public void ImplicitConversion_FromString_Works()
    {
        ShaderTagId tag = "Transparent";
        
        Assert.Equal("Transparent", tag.Name);
    }

    [Theory]
    [InlineData("Opaque", "Opaque")]
    [InlineData("Transparent", "Transparent")]
    [InlineData("", "")]
    public void ImplicitConversion_WorksWithVariousStrings(string input, string expected)
    {
        ShaderTagId tag = input;
        
        Assert.Equal(expected, tag.Name);
    }

    [Fact]
    public void Equals_SameName_ReturnsTrue()
    {
        var tag1 = new ShaderTagId("Test");
        var tag2 = new ShaderTagId("Test");
        
        Assert.True(tag1 == tag2);
        Assert.True(tag1.Equals(tag2));
    }

    [Fact]
    public void Equals_DifferentName_ReturnsFalse()
    {
        var tag1 = new ShaderTagId("Test1");
        var tag2 = new ShaderTagId("Test2");
        
        Assert.False(tag1 == tag2);
        Assert.False(tag1.Equals(tag2));
    }

    [Fact]
    public void GetHashCode_SameName_SameHashCode()
    {
        var tag1 = new ShaderTagId("Test");
        var tag2 = new ShaderTagId("Test");
        
        Assert.Equal(tag1.GetHashCode(), tag2.GetHashCode());
    }
}
