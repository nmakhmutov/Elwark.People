using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class RegionCodeTests
{
    [Theory]
    [InlineData("AF")]
    [InlineData("AN")]
    [InlineData("AS")]
    [InlineData("EU")]
    [InlineData("NA")]
    [InlineData("OC")]
    [InlineData("SA")]
    [InlineData("eu")]
    public void Parse_KnownCode_Succeeds(string code)
    {
        var region = RegionCode.Parse(code);
        Assert.Equal(code, region.Value);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("US")]
    [InlineData("")]
    public void Parse_UnknownCode_Throws(string code)
    {
        Assert.Throws<FormatException>(() => RegionCode.Parse(code));
    }

    [Fact]
    public void Empty_IsDoubleDash()
    {
        Assert.Equal("--", RegionCode.Empty.Value);
    }

    [Fact]
    public void IsEmpty_Empty_ReturnsTrue()
    {
        Assert.True(RegionCode.Empty.IsEmpty());
    }
}
