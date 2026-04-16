using System.Globalization;
using System.Net;
using NSubstitute;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace Integration.Shared.Tests.Infrastructure;

public static class AccountTestFactory
{
    public static readonly Locale EnLocale = Locale.Parse("en");

    public static TimeProvider FixedUtc(DateTime utc)
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(utc, TimeSpan.Zero));
        return tp;
    }

    public static Account CreateNewAccount(IIpHasher hasher, TimeProvider clock, string nickname = "integration")
    {
        var account = Account.Create(Timezone.Utc, EnLocale, IPAddress.Loopback, hasher, clock);
        account.Update(
            Name.Create(Nickname.Parse(nickname)),
            account.Picture,
            account.Locale,
            account.Region,
            account.Country,
            account.Timezone,
            clock);
        account.ClearDomainEvents();
        return account;
    }

    public static IIpHasher FakeIpHasher()
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>())
        .Returns([
            1,
            2,
            3,
            4
        ]);
        return hasher;
    }
}
