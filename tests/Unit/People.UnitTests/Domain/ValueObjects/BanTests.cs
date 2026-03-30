using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.ValueObjects;

public sealed class BanTests
{
    [Fact]
    public void Ctor_SetsAllProperties()
    {
        var expired = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var ban = new Ban("spam", expired, created);

        Assert.Equal("spam", ban.Reason);
        Assert.Equal(expired, ban.ExpiredAt);
        Assert.Equal(created, ban.CreatedAt);
    }

    [Fact]
    public void Equals_SameValues_True()
    {
        var expired = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Ban("r", expired, created);
        var b = new Ban("r", expired, created);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentReason_False()
    {
        var expired = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Ban("a", expired, created);
        var b = new Ban("b", expired, created);
        Assert.True(a != b);
    }
}
