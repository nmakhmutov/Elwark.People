using NSubstitute;
using People.Application.Commands.UpdateAccount;
using People.Application.Providers.Country;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class UpdateAccountCommandTests
{
    private static readonly AccountId AccountId = new(501L);
    private static readonly DateTime Utc = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_UpdatesProfileUsesCountryClientRegionAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var country = CountryCode.Parse("DE");
        var regionEu = RegionCode.Parse("EU");
        var language = Language.Parse("de");
        var tz = TimeZone.Utc;
        var dateFmt = DateFormat.Parse("dd.MM.yyyy");
        var timeFmt = TimeFormat.Parse("HH:mm:ss");

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var countryClient = Substitute.For<ICountryClient>();
        countryClient.GetAsync(country, Arg.Any<CancellationToken>())
            .Returns(new CountryDetails("276", "DE", "DEU", regionEu));

        var handler = new UpdateAccountCommandHandler(repo, countryClient, time);

        var cmd = new UpdateAccountCommand(
            AccountId,
            FirstName: "Ada",
            LastName: "Lovelace",
            Nickname: Nickname.Parse("ada"),
            UseNickname: false,
            Language: language,
            TimeZone: tz,
            DateFormat: dateFmt,
            TimeFormat: timeFmt,
            StartOfWeek: DayOfWeek.Monday,
            Country: country);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Same(account, result);
        Assert.Equal(Nickname.Parse("ada"), result.Name.Nickname);
        Assert.Equal("Ada", result.Name.FirstName);
        Assert.Equal("Lovelace", result.Name.LastName);
        Assert.False(result.Name.UseNickname);
        Assert.Equal(language, result.Language);
        Assert.Equal(tz, result.TimeZone);
        Assert.Equal(dateFmt, result.DateFormat);
        Assert.Equal(timeFmt, result.TimeFormat);
        Assert.Equal(DayOfWeek.Monday, result.StartOfWeek);
        Assert.Equal(country, result.Country);
        Assert.Equal(regionEu, result.Region);
        await countryClient.Received(1).GetAsync(country, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CountryClientReturnsNull_UsesEmptyRegion()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var country = CountryCode.Parse("US");

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var countryClient = Substitute.For<ICountryClient>();
        countryClient.GetAsync(country, Arg.Any<CancellationToken>()).Returns((CountryDetails?)null);

        var handler = new UpdateAccountCommandHandler(repo, countryClient, time);

        await handler.Handle(
            new UpdateAccountCommand(
                AccountId,
                null,
                null,
                Nickname.Parse("nick"),
                UseNickname: true,
                Language.Parse("en"),
                TimeZone.Utc,
                DateFormat.Default,
                TimeFormat.Default,
                DayOfWeek.Sunday,
                country),
            CancellationToken.None);

        Assert.Equal(RegionCode.Empty, account.Region);
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new UpdateAccountCommandHandler(repo, Substitute.For<ICountryClient>(), TimeProvider.System);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(
                new UpdateAccountCommand(
                    AccountId,
                    null,
                    null,
                    Nickname.Parse("x"),
                    true,
                    Language.Parse("en"),
                    TimeZone.Utc,
                    DateFormat.Default,
                    TimeFormat.Default,
                    DayOfWeek.Monday,
                    CountryCode.Parse("US")),
                CancellationToken.None));
    }
}
