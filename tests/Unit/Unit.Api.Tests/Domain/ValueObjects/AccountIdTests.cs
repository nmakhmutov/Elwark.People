using People.Domain.Entities;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class AccountIdTests
{
    [Fact]
    public void ImplicitToLong_ReturnsValue()
    {
        AccountId id = new(42L);
        long value = id;
        Assert.Equal(42L, value);
    }

    [Fact]
    public void ImplicitFromLong_RoundTrips()
    {
        AccountId id = 99L;
        Assert.Equal(99L, (long)id);
    }

    [Fact]
    public void Equals_Operators_MatchValue()
    {
        var a = new AccountId(1L);
        var b = new AccountId(1L);
        var c = new AccountId(2L);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.False(a == c);
    }

    [Fact]
    public void CompareTo_OrdersByValue()
    {
        Assert.True(new AccountId(1L).CompareTo(new AccountId(2L)) < 0);
        Assert.True(new AccountId(3L).CompareTo(new AccountId(2L)) > 0);
        Assert.Equal(0, new AccountId(5L).CompareTo(new AccountId(5L)));
    }

    [Fact]
    public void Default_IsZero()
    {
        Assert.Equal(new AccountId(0L), default(AccountId));
        Assert.Equal(0L, (long)default(AccountId));
    }
}
