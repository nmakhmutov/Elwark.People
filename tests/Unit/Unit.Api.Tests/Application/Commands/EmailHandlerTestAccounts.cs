using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace Unit.Api.Tests.Application.Commands;

internal static class EmailHandlerTestAccounts
{
    private static readonly IPAddress Ip = IPAddress.Loopback;

    internal static TimeProvider FixedTime(DateTime utc)
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(utc, TimeSpan.Zero));
        return tp;
    }

    internal static void SetAccountId(Account account, AccountId id) =>
        typeof(Entity<AccountId>).GetProperty(nameof(Entity<>.Id))!
            .SetValue(account, id);

    internal static Account AccountWithConfirmedPrimary(
        AccountId id,
        TimeProvider time,
        string primaryEmail = "primary@test.com"
    )
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1]);
        var account = Account.Create(Language.Parse("en"), Ip, hasher, time);
        SetAccountId(account, id);
        account.AddEmail(new MailAddress(primaryEmail), true, time);
        account.ClearDomainEvents();
        return account;
    }

    internal static Account AccountWithTwoConfirmedEmails(
        AccountId id,
        TimeProvider time,
        string first = "first@test.com",
        string second = "second@test.com"
    )
    {
        var account = AccountWithConfirmedPrimary(id, time, first);
        account.AddEmail(new MailAddress(second), true, time);
        account.ClearDomainEvents();
        return account;
    }

    internal static Account AccountWithUnconfirmedExtra(
        AccountId id,
        TimeProvider time,
        string primary = "primary@test.com",
        string pending = "pending@test.com"
    )
    {
        var account = AccountWithConfirmedPrimary(id, time, primary);
        account.AddEmail(new MailAddress(pending), false, time);
        account.ClearDomainEvents();
        return account;
    }

    internal static Account AccountWithUnconfirmedPrimary(
        AccountId id,
        TimeProvider time,
        string email = "signup@test.com"
    )
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1]);
        var account = Account.Create(Language.Parse("en"), Ip, hasher, time);
        SetAccountId(account, id);
        account.AddEmail(new MailAddress(email), false, time);
        account.ClearDomainEvents();
        return account;
    }
}
