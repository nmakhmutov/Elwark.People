using TimeZoneVo = People.Domain.ValueObjects.TimeZone;
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

    [Theory]
    [InlineData("Not/A/Real/Zone")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_InvalidId_Throws(string id)
    {
        Assert.ThrowsAny<Exception>(() => TimeZoneVo.Parse(id));
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
    public void TryParse_Invalid_Throws()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneVo.TryParse("Not/A/Real/Zone", out _));
    }

    [Fact]
    public void Utc_MatchesSystemUtc()
    {
        Assert.Equal(TimeZoneInfo.Utc.Id, TimeZoneVo.Utc.Value);
    }
}
