using System.Net;
using System.Net.Mail;
using Mediator;
using People.Application.Queries.GetAccountSummary;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using Xunit;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.IntegrationTests.Queries;

public sealed class GetAccountSummaryQueryTests(PostgreSqlFixture postgres) : QueryIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Handle_MapsRowToAccountSummary_IncludingRolesAndBan()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var primary = new MailAddress("summary-primary@example.com");
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc));
        var banExpires = fixedTime.GetUtcNow().UtcDateTime.AddDays(14);

        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
            account.ClearDomainEvents();
            account.Update(
                Name.Create(Nickname.Parse("SumNick"), "Sum", "Mary", preferNickname: false),
                Picture.Parse("https://summary.example/p.png"),
                Language.Parse("ru"),
                RegionCode.Parse("EU"),
                CountryCode.Parse("FR"),
                TimeZone.Parse("Europe/Paris"),
                DateFormat.Parse("yyyy-MM-dd"),
                TimeFormat.Parse("HH:mm"),
                DayOfWeek.Tuesday,
                fixedTime);
            account.AddEmail(primary, true, fixedTime);
            account.AddRole("member", fixedTime);
            account.AddRole("admin", fixedTime);
            account.Ban("policy", banExpires, fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetAccountSummaryQuery(id), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal(primary.Address, result.Email);
        Assert.Equal(Nickname.Parse("SumNick"), result.Name.Nickname);
        Assert.Equal("Sum", result.Name.FirstName);
        Assert.Equal("Mary", result.Name.LastName);
        Assert.False(result.Name.PreferNickname);
        Assert.Equal("Sum Mary", result.Name.FullName());
        Assert.Equal(Picture.Parse("https://summary.example/p.png"), result.Picture);
        Assert.Equal(Language.Parse("ru"), result.Language);
        Assert.Equal(RegionCode.Parse("EU"), result.RegionCode);
        Assert.Equal(CountryCode.Parse("FR"), result.CountryCode);
        Assert.Equal(TimeZone.Parse("Europe/Paris"), result.TimeZone);
        Assert.Equal(DateFormat.Parse("yyyy-MM-dd"), result.DateFormat);
        Assert.Equal(TimeFormat.Parse("HH:mm"), result.TimeFormat);
        Assert.Equal(DayOfWeek.Tuesday, result.StartOfWeek);
        Assert.Equal(new[] { "member", "admin" }, result.Roles);

        Assert.NotNull(result.Ban);
        Assert.Equal("policy", result.Ban!.Reason);
        Assert.Equal(banExpires, result.Ban.ExpiredAt, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Handle_NoAccountOrNoPrimaryEmail_ThrowsNotFound()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<AccountException>(() =>
            sender.Send(new GetAccountSummaryQuery(new AccountId(8_888_777_666L)), CancellationToken.None).AsTask());

        Assert.Equal("NotFound", ex.Code);
    }
}
