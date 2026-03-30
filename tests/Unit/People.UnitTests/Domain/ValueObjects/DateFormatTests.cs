using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.ValueObjects;

public sealed class DateFormatTests
{
    public static TheoryData<string> ValidFormats =>
    [
        "MM.dd.yyyy",
        "dd.MM.yyyy",
        "dd.MM.yy",
        "d.M.yyyy",
        "d.M.yy",
        "MM-dd-yyyy",
        "dd-MM-yyyy",
        "dd-MM-yy",
        "d-M-yyyy",
        "d-M-yy",
        "MM/dd/yyyy",
        "dd/MM/yyyy",
        "dd/MM/yy",
        "d/M/yyyy",
        "d/M/yy",
        "yyyy-MM-dd"
    ];

    [Theory]
    [MemberData(nameof(ValidFormats))]
    public void Parse_AllowedToken_Succeeds(string format)
    {
        var df = DateFormat.Parse(format);
        Assert.Equal(format, df.Value);
    }

    [Theory]
    [InlineData("not-a-format")]
    [InlineData("yyyy/MM/dd")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_UnknownToken_Throws(string format)
    {
        Assert.Throws<FormatException>(() => DateFormat.Parse(format));
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        var ok = DateFormat.TryParse("yyyy-MM-dd", out var df);
        Assert.True(ok);
        Assert.Equal("yyyy-MM-dd", df.Value);
    }

    [Fact]
    public void TryParse_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => DateFormat.TryParse("bad", out _));
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var ok = DateFormat.TryParse(null, out var result);
        Assert.False(ok);
        Assert.True(result == default(DateFormat));
    }

    [Fact]
    public void Default_IsIsoDate()
    {
        Assert.Equal("yyyy-MM-dd", DateFormat.Default.Value);
    }
}
