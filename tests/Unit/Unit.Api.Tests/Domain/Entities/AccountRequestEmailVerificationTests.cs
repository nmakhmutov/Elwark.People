using System.Net.Mail;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Domain.Exceptions;
using Unit.Api.Tests.Application.Commands;
using Xunit;

namespace Unit.Api.Tests.Domain.Entities;

public sealed class AccountRequestEmailVerificationTests
{
    private static readonly AccountId AccountId = new(500L);
    private static readonly DateTime Utc = new(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequestEmailVerification_ValidUnconfirmedEmail_RaisesEvent()
    {
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, EmailHandlerTestAccounts.FixedTime(Utc));
        var time = EmailHandlerTestAccounts.FixedTime(Utc);

        var confirmationId = account.RequestEmailVerification(new MailAddress("pending@test.com"), time);

        Assert.NotEqual(Guid.Empty, confirmationId);
        var evt = Assert.Single(account.GetDomainEvents().OfType<EmailVerificationRequestedDomainEvent>());
        Assert.Equal(AccountId, evt.Id);
        Assert.Equal(confirmationId, evt.ConfirmationId);
        Assert.Equal("pending@test.com", evt.Email.Address);
        Assert.Equal(Utc, evt.OccurredAt);
    }

    [Fact]
    public void RequestEmailVerification_EmailNotOnAccount_ThrowsEmailException()
    {
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, EmailHandlerTestAccounts.FixedTime(Utc));
        var time = EmailHandlerTestAccounts.FixedTime(Utc);

        Assert.Throws<EmailException>(() =>
            account.RequestEmailVerification(new MailAddress("notfound@test.com"), time));
    }

    [Fact]
    public void RequestEmailVerification_AlreadyConfirmedEmail_ThrowsEmailException()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time, "confirmed@test.com");

        Assert.Throws<EmailException>(() =>
            account.RequestEmailVerification(new MailAddress("confirmed@test.com"), time));
    }

    [Fact]
    public void RequestEmailVerification_ReturnedGuid_MatchesEventConfirmationId()
    {
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId,
            EmailHandlerTestAccounts.FixedTime(Utc),
            pending: "match@test.com");
        var time = EmailHandlerTestAccounts.FixedTime(Utc);

        var confirmationId = account.RequestEmailVerification(new MailAddress("match@test.com"), time);

        var evt = Assert.Single(account.GetDomainEvents().OfType<EmailVerificationRequestedDomainEvent>());
        Assert.Equal(confirmationId, evt.ConfirmationId);
    }
}
