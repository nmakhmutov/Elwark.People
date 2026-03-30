using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.ValueObjects;

public sealed class CountryCodeTests
{
    [Theory]
    [InlineData("US", "US")]
    [InlineData("de", "DE")]
    [InlineData("Gb", "GB")]
    public void Parse_IsoCode_Uppercases(string input, string expected)
    {
        Assert.Equal(expected, CountryCode.Parse(input).Value);
    }

    [Theory]
    [InlineData("USA")]
    [InlineData("U")]
    [InlineData("")]
    [InlineData("A")]
    public void Parse_InvalidLength_Throws(string code)
    {
        Assert.Throws<FormatException>(() => CountryCode.Parse(code));
    }

    [Fact]
    public void Empty_IsDoubleDash()
    {
        Assert.Equal("--", CountryCode.Empty.Value);
    }

    [Fact]
    public void IsEmpty_Empty_ReturnsTrue()
    {
        Assert.True(CountryCode.Empty.IsEmpty());
    }

    [Fact]
    public void IsEmpty_WithCode_ReturnsFalse()
    {
        Assert.False(CountryCode.Parse("US").IsEmpty());
    }
}
