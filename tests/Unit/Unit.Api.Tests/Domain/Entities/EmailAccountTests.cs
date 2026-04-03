using People.Domain.Entities;
using Xunit;

namespace Unit.Api.Tests.Domain.Entities;

public sealed class EmailAccountTests
{
    private static readonly AccountId AccountId = new(1L);
    private static readonly DateTime CreatedAt = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_IsPrimaryTrue_SetsPrimary()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", isPrimary: true, null, CreatedAt);

        Assert.True(email.IsPrimary);
    }

    [Fact]
    public void Create_IsPrimaryFalse_NotPrimary()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", isPrimary: false, null, CreatedAt);

        Assert.False(email.IsPrimary);
    }

    [Fact]
    public void Create_ConfirmedAtNull_NotConfirmed()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", true, confirmedAt: null, CreatedAt);

        Assert.False(email.IsConfirmed);
    }

    [Fact]
    public void Create_ConfirmedAtSet_IsConfirmed()
    {
        var confirmed = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var email = EmailAccount.Create(AccountId, "a@b.co", true, confirmed, CreatedAt);

        Assert.True(email.IsConfirmed);
    }

    [Fact]
    public void SetPrimary_WasFalse_ReturnsTrueAndPrimary()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", false, null, CreatedAt);

        var returned = email.SetPrimary();

        Assert.True(returned);
        Assert.True(email.IsPrimary);
    }

    [Fact]
    public void RemovePrimary_WasTrue_ReturnsFalseAndNotPrimary()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", true, null, CreatedAt);

        var returned = email.RemovePrimary();

        Assert.False(returned);
        Assert.False(email.IsPrimary);
    }

    [Fact]
    public void Confirm_Unconfirmed_BecomesConfirmed()
    {
        var email = EmailAccount.Create(AccountId, "a@b.co", true, null, CreatedAt);
        var at = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        email.Confirm(at);

        Assert.True(email.IsConfirmed);
    }

    [Fact]
    public void Confute_Confirmed_BecomesUnconfirmed()
    {
        var confirmed = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var email = EmailAccount.Create(AccountId, "a@b.co", true, confirmed, CreatedAt);
        Assert.True(email.IsConfirmed);

        email.Confute();

        Assert.False(email.IsConfirmed);
    }
}
