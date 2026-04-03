using System.Net;
using System.Net.Mail;
using Grpc.Core;
using Mediator;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;
using DomainLanguage = People.Domain.ValueObjects.Language;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.IntegrationTests.Grpc;

public sealed class PeopleServiceGetAccountTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task GetAccount_ReturnsAccountReply_WhenSeeded()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var primary = new MailAddress("grpc-get@example.com");
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(
                "grpc-nick",
                DomainLanguage.Parse("en"),
                IPAddress.Loopback,
                hasher,
                fixedTime
            );

            account.ClearDomainEvents();
            account.Update(
                Name.Create("GrpcNick", "G", "User", preferNickname: false),
                Picture.Parse("https://grpc.example/p.png"),
                DomainLanguage.Parse("de"),
                RegionCode.Parse("EU"),
                CountryCode.Parse("AT"),
                TimeZone.Parse("Europe/Vienna"),
                DateFormat.Default,
                TimeFormat.Default,
                DayOfWeek.Monday,
                fixedTime
            );

            account.AddEmail(primary, true, fixedTime);
            account.AddRole("viewer", fixedTime);

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
            static (s, req, ctx) => s.GetAccount(req, ctx));

        Assert.Equal((long)id, reply.Id);
        Assert.Equal(primary.Address, reply.Email);
        Assert.Equal("GrpcNick", reply.Nickname);
        Assert.Equal("G", reply.FirstName);
        Assert.Equal("User", reply.LastName);
        Assert.Equal("G User", reply.FullName);
        Assert.Equal("https://grpc.example/p.png", reply.Picture);
        Assert.Equal("AT", reply.CountryCode);
        Assert.Equal("Europe/Vienna", reply.TimeZone);
        Assert.Equal("de", reply.Language.Value);
        Assert.Single(reply.Roles);
        Assert.Equal("viewer", reply.Roles[0]);
        Assert.Null(reply.Ban);
    }

    [Fact]
    public async Task GetAccount_UnknownId_ThrowsRpcNotFound()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new AccountRequest { Id = 42_424_242L },
                static (s, req, ctx) => s.GetAccount(req, ctx)));

        Assert.Equal(StatusCode.NotFound, rpc.Status.StatusCode);
    }
}
