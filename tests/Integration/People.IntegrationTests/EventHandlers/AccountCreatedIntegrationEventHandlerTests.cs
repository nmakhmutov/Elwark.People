using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.EnrichAccount;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using TimeZone = People.Domain.ValueObjects.TimeZone;
using Xunit;

namespace People.IntegrationTests.EventHandlers;

public sealed class AccountCreatedIntegrationEventHandlerTests(PostgreSqlFixture postgres)
    : IntegrationEventHandlerIntegrationTestBase(postgres)
{
    [Fact]
    public async Task HandleAsync_WhenIpAndGravatarReturnData_PersistsLocationAndProfile()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        Fx.Ip1.GetAsync("203.0.113.10", "en")
            .Returns(new IpInformation("US", "NA", "NYC", TimeZoneInfo.Utc.Id));
        Fx.Ip2.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);

        Fx.Gravatar
            .GetAsync(Arg.Any<MailAddress>())
            .Returns(
                new GravatarProfile
                {
                    ThumbnailUrl = "https://www.gravatar.com/avatar/test?s=80",
                    Name = [new GravatarProfile.NameData { FirstName = "Jane", LastName = "Doe" }]
                }
            );

        AccountId accountId;
        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("created-handler@example.com"),
                nickname: "nick"
            );
        }

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<EnrichAccountCommandHandler>();
            await handler.Handle(new EnrichAccountCommand(accountId, "203.0.113.10"), CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var repo = readScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(account);
            Assert.Equal(CountryCode.Parse("US"), account.Country);
            Assert.Equal(RegionCode.Parse("NA"), account.Region);
            Assert.Equal(TimeZone.Utc, account.TimeZone);
            Assert.Equal("Jane", account.Name.FirstName);
            Assert.Equal("Doe", account.Name.LastName);
            Assert.Equal(Picture.Parse("https://www.gravatar.com/avatar/test?s=80"), account.Picture);
        }

        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAllIpProvidersReturnNull_DoesNotChangeLocation_AndCompletes()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        AccountId accountId;
        CountryCode expectedCountry;
        RegionCode expectedRegion;
        TimeZone expectedTz;

        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("no-ip@example.com")
            );
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var seeded = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(seeded);
            expectedCountry = seeded.Country;
            expectedRegion = seeded.Region;
            expectedTz = seeded.TimeZone;
        }

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<EnrichAccountCommandHandler>();
            await handler.Handle(new EnrichAccountCommand(accountId, "198.51.100.1"), CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var repo = readScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(account);
            Assert.Equal(expectedCountry, account.Country);
            Assert.Equal(expectedRegion, account.Region);
            Assert.Equal(expectedTz, account.TimeZone);
        }

        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenGravatarReturnsNull_DoesNotChangeNameOrPicture()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        Fx.Ip1.GetAsync("203.0.113.20", "en")
            .Returns(new IpInformation("CA", "NA", "YYZ", TimeZoneInfo.Utc.Id));

        AccountId accountId;
        Picture expectedPicture;
        string? expectedFirst;
        string? expectedLast;

        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("no-gravatar@example.com"),
                nickname: "seed-nick"
            );
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var seeded = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(seeded);
            expectedPicture = seeded.Picture;
            expectedFirst = seeded.Name.FirstName;
            expectedLast = seeded.Name.LastName;
        }

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<EnrichAccountCommandHandler>();
            await handler.Handle(new EnrichAccountCommand(accountId, "203.0.113.20"), CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var repo = readScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(account);
            Assert.Equal(expectedPicture, account.Picture);
            Assert.Equal(expectedFirst, account.Name.FirstName);
            Assert.Equal(expectedLast, account.Name.LastName);
            Assert.Equal(CountryCode.Parse("CA"), account.Country);
        }

        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenIpProviderThrows_ContinuesWithoutLocation_AndDoesNotThrow()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        Fx.Ip1.GetAsync("198.51.100.99", "en")
            .Returns(Task.FromException<IpInformation?>(new InvalidOperationException("provider down")));
        Fx.Ip2.GetAsync("198.51.100.99", "en").Returns((IpInformation?)null);

        AccountId accountId;
        CountryCode expectedCountry;

        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("ip-throw@example.com")
            );
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var seeded = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(seeded);
            expectedCountry = seeded.Country;
        }

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<EnrichAccountCommandHandler>();
            await handler.Handle(new EnrichAccountCommand(accountId, "198.51.100.99"), CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var repo = readScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await repo.GetAsync(accountId, CancellationToken.None);
            Assert.NotNull(account);
            Assert.Equal(expectedCountry, account.Country);
        }

        await Fx.Ip2.Received(1).GetAsync("198.51.100.99", "en");
        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }
}
