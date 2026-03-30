using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using Xunit;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.IntegrationTests.Infrastructure;

[Collection(nameof(PostgresCollection))]
public sealed class EntityConfigurationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Account_RoundTrips_ValueObjects_Name_OwnedFields_AndRelations()
    {
        var utc = new DateTime(2026, 5, 10, 12, 30, 0, DateTimeKind.Utc);
        var time = AccountTestFactory.FixedUtc(utc);
        var mediator = new NoOpMediator();

        await using (var write = fixture.CreateContext(mediator))
        {
            await IntegrationDatabaseCleanup.DeleteAllAsync(write);

            var draft = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), "cfg");
            write.Accounts.Add(draft);
            await write.SaveEntitiesAsync(CancellationToken.None);

            draft.AddEmail(new MailAddress("cfg@example.com"), true, time);
            await write.SaveEntitiesAsync(CancellationToken.None);

            draft.Update(
                Language.Parse("de"),
                RegionCode.Parse("EU"),
                CountryCode.Parse("US"),
                TimeZone.Parse("Europe/Berlin"));
            draft.Update("nick", "Ada", "Lovelace", false);
            draft.Update(DateFormat.Parse("dd.MM.yyyy"), TimeFormat.Parse("H:mm"), DayOfWeek.Friday);
            draft.AddRole("admin");
            draft.Ban("reason", new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), time);
            draft.AddGoogle("g-1", "G", "X", time);
            await write.SaveEntitiesAsync(CancellationToken.None);
        }

        await using var read = fixture.CreateContext(new NoOpMediator());
        var account = await read.Accounts
            .Include(a => a.Emails)
            .Include(a => a.Externals)
            .AsSplitQuery()
            .SingleAsync();

        Assert.Equal(Language.Parse("de"), account.Language);
        Assert.Equal(RegionCode.Parse("EU"), account.RegionCode);
        Assert.Equal(CountryCode.Parse("US"), account.CountryCode);
        Assert.Equal(TimeZone.Parse("Europe/Berlin"), account.TimeZone);
        Assert.Equal(DateFormat.Parse("dd.MM.yyyy"), account.DateFormat);
        Assert.Equal(TimeFormat.Parse("H:mm"), account.TimeFormat);
        Assert.Equal(DayOfWeek.Friday, account.StartOfWeek);

        Assert.Equal("nick", account.Name.Nickname);
        Assert.Equal("Ada", account.Name.FirstName);
        Assert.Equal("Lovelace", account.Name.LastName);
        Assert.False(account.Name.PreferNickname);

        var roles = AccountPrivateState.Roles(account);
        Assert.Contains("admin", roles);

        var ban = AccountPrivateState.Ban(account);
        Assert.NotNull(ban);
        Assert.Equal("reason", ban.Reason);

        Assert.True(AccountPrivateState.CreatedAt(account) != default);
        Assert.True(AccountPrivateState.UpdatedAt(account) != default);
        Assert.True(AccountPrivateState.LastLogIn(account) != default);

        var email = Assert.Single(account.Emails.ToList());
        Assert.Equal(account.Id, email.AccountId);
        Assert.Equal("cfg@example.com", email.Email);

        var ext = Assert.Single(account.Externals.ToList());
        Assert.Equal(ExternalService.Google, ext.Type);
        Assert.Equal("g-1", ext.Identity);
        Assert.Equal("G", ext.FirstName);
        Assert.Equal("X", ext.LastName);
    }

    [Fact]
    public async Task ExternalConnection_Microsoft_PersistsEnumByte()
    {
        var time = AccountTestFactory.FixedUtc(new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc));
        var mediator = new NoOpMediator();

        await using (var write = fixture.CreateContext(mediator))
        {
            await IntegrationDatabaseCleanup.DeleteAllAsync(write);

            var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), "ms");
            write.Accounts.Add(account);
            await write.SaveEntitiesAsync(CancellationToken.None);

            account.AddEmail(new MailAddress("ms@example.com"), true, time);
            account.AddMicrosoft("live:123", "Pat", "Kim", time);
            await write.SaveEntitiesAsync(CancellationToken.None);
        }

        await using var read = fixture.CreateContext(new NoOpMediator());
        var ext = await read.Connections.SingleAsync();

        Assert.Equal(ExternalService.Microsoft, ext.Type);
        Assert.Equal("live:123", ext.Identity);
    }
}
