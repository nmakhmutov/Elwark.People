using TimeZoneVo = People.Domain.ValueObjects.Timezone;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class TimeZoneTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/Moscow")]
    public void Parse_ValidId_Succeeds(string id)
    {
        var tz = TimeZoneVo.Parse(id);
        Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(id).Id, tz.Value);
    }

    [Fact]
    public void Parse_UnknownId_FallsBackToUtc()
    {
        var tz = TimeZoneVo.Parse("Not/A/Real/Zone");
        Assert.Equal(TimeZoneInfo.Utc.Id, tz.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_Throws(string id)
    {
        Assert.Throws<FormatException>(() => TimeZoneVo.Parse(id));
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        var ok = TimeZoneVo.TryParse("UTC", out var tz);
        Assert.True(ok);
        Assert.False(string.IsNullOrEmpty(tz.Value));
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var ok = TimeZoneVo.TryParse(null, out var tz);
        Assert.False(ok);
        Assert.True(tz == default(TimeZoneVo));
    }

    [Fact]
    public void TryParse_Invalid_FallsBackToUtc()
    {
        var ok = TimeZoneVo.TryParse("Not/A/Real/Zone", out var tz);
        Assert.True(ok);
        Assert.Equal(TimeZoneInfo.Utc.Id, tz.Value);
    }

    [Fact]
    public void Utc_MatchesSystemUtc()
    {
        Assert.Equal(TimeZoneInfo.Utc.Id, TimeZoneVo.Utc.Value);
    }
}
