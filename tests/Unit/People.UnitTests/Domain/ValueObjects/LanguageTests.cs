using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.ValueObjects;

public sealed class LanguageTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    [InlineData("DE")]
    public void Parse_ValidCode_Succeeds(string code)
    {
        var lang = Language.Parse(code);
        Assert.Equal(code, lang.Value);
    }

    [Theory]
    [InlineData("iv")]
    [InlineData("IV")]
    [InlineData("abc")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_InvalidCode_Throws(string code)
    {
        Assert.Throws<FormatException>(() => Language.Parse(code));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void TryParse_Valid_ReturnsTrue(string code)
    {
        var ok = Language.TryParse(code, out var result);
        Assert.True(ok);
        Assert.Equal(code, result.Value);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var ok = Language.TryParse(null, out var result);
        Assert.False(ok);
        Assert.True(result == default);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("iv")]
    [InlineData("")]
    public void TryParse_Invalid_Throws(string code)
    {
        Assert.Throws<FormatException>(() => Language.TryParse(code, out _));
    }

    [Fact]
    public void Default_IsEnglish()
    {
        Assert.Equal("en", Language.Default.Value);
    }

    [Fact]
    public void Equals_Operators_MatchValue()
    {
        var a = Language.Parse("en");
        var b = Language.Parse("en");
        var c = Language.Parse("ru");
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.False(a == c);
    }
}
