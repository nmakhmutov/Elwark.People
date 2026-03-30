using System.Net;
using System.Net.Mail;
using Mediator;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;
using DomainLanguage = People.Domain.ValueObjects.Language;

namespace People.IntegrationTests.Grpc;

public sealed class PeopleServiceIsAccountActiveTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task IsAccountActive_ReturnsTrue_ForActivatedAccount()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("grpc-active@example.com"),
                "grpc-active",
                CancellationToken.None);
        }

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new AccountRequest { Id = id },
            static (s, req, ctx) => s.IsAccountActive(req, ctx));

        Assert.True(reply.Value);
    }

    [Fact]
    public async Task IsAccountActive_ReturnsFalse_WhenNotActivated()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create("inact", DomainLanguage.Parse("en"), IPAddress.Loopback, hasher);
            account.ClearDomainEvents();
            account.AddEmail(new MailAddress("pending-grpc@example.com"), false, fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new AccountRequest { Id = id },
            static (s, req, ctx) => s.IsAccountActive(req, ctx));

        Assert.False(reply.Value);
    }

    [Fact]
    public async Task IsAccountActive_ReturnsFalse_WhenBanned()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create("ban-grpc", DomainLanguage.Parse("en"), IPAddress.Loopback, hasher);
            account.ClearDomainEvents();
            account.AddEmail(new MailAddress("banned-grpc@example.com"), true, fixedTime);
            account.Ban("policy", fixedTime.GetUtcNow().UtcDateTime.AddDays(7), fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new AccountRequest { Id = id },
            static (s, req, ctx) => s.IsAccountActive(req, ctx));

        Assert.False(reply.Value);
    }
}
