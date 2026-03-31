using System.Net;
using NSubstitute;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace People.IntegrationTests.Infrastructure;

internal static class AccountTestFactory
{
    internal static TimeProvider FixedUtc(DateTime utc)
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(utc, TimeSpan.Zero));
        return tp;
    }

    internal static Account CreateNewAccount(IIpHasher hasher, TimeProvider clock, string nickname = "integration")
    {
        var account = Account.Create(nickname, Language.Parse("en"), IPAddress.Loopback, hasher, clock);
        account.ClearDomainEvents();
        return account;
    }

    internal static IIpHasher FakeIpHasher()
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1, 2, 3, 4]);
        return hasher;
    }
}
