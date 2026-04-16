using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.UpdateAccount;
using People.Domain.Entities;
using People.Application.Providers.Country;
using People.Domain.ValueObjects;
using People.Infrastructure;
using TimeZone = People.Domain.ValueObjects.Timezone;
using Xunit;

namespace Integration.Api.Tests.Commands;

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
                UseNickname: false,
                Locale: Locale.Parse("ru"),
                Timezone: TimeZone.Parse("Europe/Berlin"),
                Country: CountryCode.Parse("DE")),
            CancellationToken.None);

        await using var read = Commands.CreateReadOnlyContext();
        var account = await read.Accounts.AsNoTracking().SingleAsync(a => a.Id == id);

        Assert.Equal(Nickname.Parse("annlee"), account.Name.Nickname);
        Assert.False(account.Name.UseNickname);
        Assert.Equal("Ann", account.Name.FirstName);
        Assert.Equal("Lee", account.Name.LastName);
        Assert.Equal(Locale.Parse("ru"), account.Locale);
        Assert.Equal(TimeZone.Parse("Europe/Berlin"), account.Timezone);
        Assert.Equal(CountryCode.Parse("DE"), account.Country);
        Assert.Equal(RegionCode.Parse("EU"), account.Region);
    }
}
