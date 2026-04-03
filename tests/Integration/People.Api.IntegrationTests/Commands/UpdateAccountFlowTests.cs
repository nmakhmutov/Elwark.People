using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.UpdateAccount;
using People.Domain.Entities;
using People.Application.Providers.Country;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;
using TimeZone = People.Domain.ValueObjects.TimeZone;
using Xunit;

namespace People.IntegrationTests.Commands;

public sealed class UpdateAccountFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task UpdateAccountCommand_PersistsAllFields()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("update@example.com"),
                CancellationToken.None
            );
        }

        Commands.Country.GetAsync(CountryCode.Parse("DE"), Arg.Any<CancellationToken>())
            .Returns(new CountryDetails("276", "DE", "DEU", RegionCode.Parse("EU")));

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        _ = await sender.Send(
            new UpdateAccountCommand(
                id,
                FirstName: "Ann",
                LastName: "Lee",
                Nickname: Nickname.Parse("annlee"),
                PreferNickname: false,
                Language: Language.Parse("ru"),
                TimeZone: TimeZone.Parse("Europe/Berlin"),
                DateFormat: DateFormat.Parse("dd.MM.yyyy"),
                TimeFormat: TimeFormat.Parse("HH:mm"),
                StartOfWeek: DayOfWeek.Wednesday,
                Country: CountryCode.Parse("DE")),
            CancellationToken.None);

        await using var read = Commands.CreateReadOnlyContext();
        var account = await read.Accounts.AsNoTracking().SingleAsync(a => a.Id == id);

        Assert.Equal(Nickname.Parse("annlee"), account.Name.Nickname);
        Assert.False(account.Name.PreferNickname);
        Assert.Equal("Ann", account.Name.FirstName);
        Assert.Equal("Lee", account.Name.LastName);
        Assert.Equal(Language.Parse("ru"), account.Language);
        Assert.Equal(TimeZone.Parse("Europe/Berlin"), account.TimeZone);
        Assert.Equal(DateFormat.Parse("dd.MM.yyyy"), account.DateFormat);
        Assert.Equal(TimeFormat.Parse("HH:mm"), account.TimeFormat);
        Assert.Equal(DayOfWeek.Wednesday, account.StartOfWeek);
        Assert.Equal(CountryCode.Parse("DE"), account.Country);
        Assert.Equal(RegionCode.Parse("EU"), account.Region);
    }
}
