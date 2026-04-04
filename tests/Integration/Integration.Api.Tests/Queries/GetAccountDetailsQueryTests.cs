using System.Globalization;
using System.Net;
using System.Net.Mail;
using Mediator;
using People.Application.Queries.GetAccountDetails;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Integration.Api.Tests.Commands;
using Xunit;
using TimeZone = People.Domain.ValueObjects.Timezone;

namespace Integration.Api.Tests.Queries;

public sealed class GetAccountDetailsQueryTests(PostgreSqlFixture postgres) : QueryIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Handle_LoadsAccountWithEmailsAndExternals_AllFieldsPopulated()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var primary = new MailAddress("details-primary@example.com");
        var secondary = new MailAddress("details-secondary@example.com");
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));

        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(
                Language.Parse("en"),
                Timezone.Utc,
                CultureInfo.InvariantCulture,
                IPAddress.Loopback,
                hasher,
                fixedTime
            );
            account.ClearDomainEvents();
            account.Update(
                Name.Create(Nickname.Parse("DisplayNick"), "First", "Last", useNickname: false),
                Picture.Parse("https://example.com/avatar.png"),
                Language.Parse("de"),
                RegionCode.Parse("EU"),
                CountryCode.Parse("DE"),
                TimeZone.Parse("Europe/Berlin"),
                DateFormat.Parse("dd.MM.yyyy"),
                TimeFormat.Parse("HH:mm"),
                DayOfWeek.Friday,
                fixedTime);
            account.AddEmail(primary, true, fixedTime);
            account.AddEmail(secondary, false, fixedTime);
            account.AddGoogle("google-sub-details", "G", "User", null, fixedTime);
            account.AddMicrosoft("ms-sub-details", "M", "Person", fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetAccountDetailsQuery(id), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal(Nickname.Parse("DisplayNick"), result.Name.Nickname);
        Assert.Equal("First", result.Name.FirstName);
        Assert.Equal("Last", result.Name.LastName);
        Assert.False(result.Name.UseNickname);
        Assert.Equal("First Last", result.Name.FullName());
        Assert.Equal(Language.Parse("de"), result.Language);
        Assert.Equal(TimeZone.Parse("Europe/Berlin"), result.Timezone);
        Assert.Equal(Picture.Parse("https://example.com/avatar.png"), result.Picture);
        Assert.Equal(RegionCode.Parse("EU"), result.Region);
        Assert.Equal(CountryCode.Parse("DE"), result.Country);
        Assert.Equal(DateFormat.Parse("dd.MM.yyyy"), result.DateFormat);
        Assert.Equal(TimeFormat.Parse("HH:mm"), result.TimeFormat);
        Assert.Equal(DayOfWeek.Friday, result.StartOfWeek);

        Assert.Equal(2, result.Emails.Count);
        Assert.Contains(result.Emails, e => e.Email == primary.Address && e.IsPrimary && e.IsConfirmed);
        Assert.Contains(result.Emails, e => e.Email == secondary.Address && !e.IsPrimary && !e.IsConfirmed);

        Assert.Equal(2, result.Externals.Count);
        Assert.Contains(result.Externals, e => e is { Type: ExternalService.Google, Identity: "google-sub-details" });
        Assert.Contains(result.Externals, e => e is { Type: ExternalService.Microsoft, Identity: "ms-sub-details" });
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsAccountNotFound()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<AccountException>(() =>
            sender.Send(new GetAccountDetailsQuery(new AccountId(9_999_888_777L)), CancellationToken.None).AsTask());

        Assert.Equal("NotFound", ex.Code);
    }
}
