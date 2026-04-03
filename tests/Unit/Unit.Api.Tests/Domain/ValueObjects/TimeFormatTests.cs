using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class TimeFormatTests
{
    public static TheoryData<string> ValidFormats =>
    [
        "H:mm",
        "HH:mm",
        "HH:mm:ss",
        "h:mm tt",
        "hh:mm tt"
    ];

    [Theory]
    [MemberData(nameof(ValidFormats))]
    public void Parse_AllowedToken_Succeeds(string format)
    {
        var tf = TimeFormat.Parse(format);
        Assert.Equal(format, tf.Value);
    }

    [Theory]
    [InlineData("HH:mm:ss.fff")]
    [InlineData("not")]
    [InlineData("")]
    public void Parse_UnknownToken_Throws(string format)
    {
        Assert.Throws<FormatException>(() => TimeFormat.Parse(format));
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        var ok = TimeFormat.TryParse("HH:mm", out var tf);
        Assert.True(ok);
        Assert.Equal("HH:mm", tf.Value);
    }

    [Fact]
    public void TryParse_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => TimeFormat.TryParse("bad", out _));
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var ok = TimeFormat.TryParse(null, out var result);
        Assert.False(ok);
        Assert.True(result == default(TimeFormat));
    }

    [Fact]
    public void Default_Is24HourHm()
    {
        Assert.Equal("HH:mm", TimeFormat.Default.Value);
    }
}
