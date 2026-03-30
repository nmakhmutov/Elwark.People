using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.IntegrationEvents.EventHandling;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure.Confirmations;
using People.UnitTests.Application.Commands;
using TimeZone = People.Domain.ValueObjects.TimeZone;
using Xunit;

namespace People.UnitTests.Application.EventHandlers;

public sealed class AccountCreatedIntegrationEventHandlerTests
{
    private static readonly DateTime Utc = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static AccountCreatedIntegrationEventHandler CreateSut(
        IAccountRepository repository,
        IEnumerable<IIpService> ipServices,
        IGravatarService gravatar,
        IConfirmationService confirmation
    ) =>
        new(
            confirmation,
            gravatar,
            ipServices,
            repository,
            NullLogger<AccountCreatedIntegrationEventHandler>.Instance);

    [Fact]
    public async Task HandleAsync_FetchesAccount_CallsIpChain_Gravatar_SavesAndDeletesConfirmation()
    {
        var accountId = new AccountId(1001L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time, "user@example.com");
        var pictureBefore = account.Picture;

        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        var ip1 = Substitute.For<IIpService>();
        var ip2 = Substitute.For<IIpService>();
        var geo = new IpInformation("DE", "EU", "Berlin", TimeZoneInfo.Utc.Id);
        ip1.GetAsync("10.0.0.1", "en").Returns((IpInformation?)null);
        ip2.GetAsync("10.0.0.1", "en").Returns(geo);

        var gravatar = Substitute.For<IGravatarService>();
        gravatar.GetAsync(Arg.Any<MailAddress>()).Returns(new GravatarProfile
        {
            ThumbnailUrl = "https://secure.gravatar.com/avatar/test.png",
            Name =
            [
                new GravatarProfile.NameData { FirstName = "Ada", LastName = "Lovelace" }
            ]
        });

        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip1, ip2], gravatar, confirmation);
        var message = new AccountCreatedIntegrationEvent(1001L, "10.0.0.1");

        await sut.HandleAsync(message, CancellationToken.None);

        await repository.Received(1).GetAsync(accountId, Arg.Any<CancellationToken>());
        await ip1.Received(1).GetAsync("10.0.0.1", "en");
        await ip2.Received(1).GetAsync("10.0.0.1", "en");
        await gravatar.Received(1).GetAsync(Arg.Is<MailAddress>(m => m.Address == "user@example.com"));

        Assert.Equal(CountryCode.Parse("DE"), account.CountryCode);
        Assert.Equal(RegionCode.Parse("EU"), account.RegionCode);
        Assert.Equal(TimeZone.Utc, account.TimeZone);
        Assert.Equal("Ada", account.Name.FirstName);
        Assert.Equal("Lovelace", account.Name.LastName);
        Assert.NotEqual(pictureBefore, account.Picture);

        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAccountMissing_ReturnsEarly_WithoutSaveOrConfirmationDelete()
    {
        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);

        var ip = Substitute.For<IIpService>();
        var gravatar = Substitute.For<IGravatarService>();
        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip], gravatar, confirmation);

        await sut.HandleAsync(new AccountCreatedIntegrationEvent(999L, "8.8.8.8"), CancellationToken.None);

        await ip.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<string>());
        await gravatar.DidNotReceive().GetAsync(Arg.Any<MailAddress>());
        await uow.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.DidNotReceive().DeleteAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAllIpServicesReturnNull_SkipsGeoUpdate_StillCallsGravatarSaveAndDelete()
    {
        var accountId = new AccountId(1002L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);
        var regionBefore = account.RegionCode;

        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        var ip1 = Substitute.For<IIpService>();
        var ip2 = Substitute.For<IIpService>();
        ip1.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);
        ip2.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);

        var gravatar = Substitute.For<IGravatarService>();
        gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);

        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip1, ip2], gravatar, confirmation);

        await sut.HandleAsync(new AccountCreatedIntegrationEvent(1002L, "192.0.2.1"), CancellationToken.None);

        Assert.Equal(regionBefore, account.RegionCode);
        await ip1.Received(1).GetAsync("192.0.2.1", "en");
        await ip2.Received(1).GetAsync("192.0.2.1", "en");
        await gravatar.Received(1).GetAsync(Arg.Any<MailAddress>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenGravatarReturnsNull_DoesNotChangeNameOrPicture()
    {
        var accountId = new AccountId(1003L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);
        var nameBefore = account.Name.FullName();
        var pictureBefore = account.Picture;

        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        var ip = Substitute.For<IIpService>();
        ip.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);

        var gravatar = Substitute.For<IGravatarService>();
        gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);

        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip], gravatar, confirmation);

        await sut.HandleAsync(new AccountCreatedIntegrationEvent(1003L, "203.0.113.9"), CancellationToken.None);

        Assert.Equal(nameBefore, account.Name.FullName());
        Assert.Equal(pictureBefore, account.Picture);
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StopsIpChainAfterFirstNonNullResult()
    {
        var accountId = new AccountId(1004L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);

        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        var ip1 = Substitute.For<IIpService>();
        var ip2 = Substitute.For<IIpService>();
        ip1.GetAsync("1.1.1.1", "en").Returns(new IpInformation("FR", "EU", "Paris", TimeZoneInfo.Utc.Id));

        var gravatar = Substitute.For<IGravatarService>();
        gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);
        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip1, ip2], gravatar, confirmation);

        await sut.HandleAsync(new AccountCreatedIntegrationEvent(1004L, "1.1.1.1"), CancellationToken.None);

        await ip1.Received(1).GetAsync("1.1.1.1", "en");
        await ip2.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WhenIpServiceThrows_ContinuesWithNextProvider()
    {
        var accountId = new AccountId(1005L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);

        var repository = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repository.UnitOfWork.Returns(uow);
        repository.GetAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        var ip1 = Substitute.For<IIpService>();
        ip1.GetAsync("5.5.5.5", "en")
            .Returns(Task.FromException<IpInformation?>(new InvalidOperationException("provider down")));

        var ip2 = Substitute.For<IIpService>();
        ip2.GetAsync("5.5.5.5", "en")
            .Returns(new IpInformation("CA", "NA", "Toronto", TimeZoneInfo.Utc.Id));

        var gravatar = Substitute.For<IGravatarService>();
        gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);
        var confirmation = Substitute.For<IConfirmationService>();

        var sut = CreateSut(repository, [ip1, ip2], gravatar, confirmation);

        await sut.HandleAsync(new AccountCreatedIntegrationEvent(1005L, "5.5.5.5"), CancellationToken.None);

        await ip2.Received(1).GetAsync("5.5.5.5", "en");
        Assert.Equal(CountryCode.Parse("CA"), account.CountryCode);
        Assert.Equal(RegionCode.Parse("NA"), account.RegionCode);
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }
}
