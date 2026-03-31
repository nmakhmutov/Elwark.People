using System.Net;
using System.Net.Mail;
using Grpc.Core;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using People.Api.Infrastructure.Interceptors;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using DomainLanguage = People.Domain.ValueObjects.Language;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;

namespace People.IntegrationTests.Grpc;

public sealed class GrpcExceptionInterceptorIntegrationTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task AccountException_NotFound_MapsToRpcNotFound_ThroughInterceptor()
    {
        var interceptor = CreateInterceptor();
        var context = CreateCallContext();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(AccountException.NotFound(new AccountId(404)))));

        Assert.Equal(StatusCode.NotFound, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task EmailException_NotConfirmed_MapsToRpcFailedPrecondition_ThroughPeopleService()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create("unconfirmed-grpc", DomainLanguage.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
            account.ClearDomainEvents();
            account.AddEmail(new MailAddress("not-confirmed-grpc@example.com"), false, fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
        }

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new EmailSigningInRequest
                {
                    Email = GrpcProtoTestData.Email("not-confirmed-grpc@example.com"),
                    Language = GrpcProtoTestData.EnLanguage()
                },
                static (s, req, ctx) => s.SigningInByEmail(req, ctx)));

        Assert.Equal(StatusCode.FailedPrecondition, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_MapsToRpcInternal_ThroughInterceptor()
    {
        var interceptor = new GrpcExceptionInterceptor(NullLoggerFactory.Instance);
        var context = CreateCallContext();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(new InvalidOperationException("unexpected"))));

        Assert.Equal(StatusCode.Internal, rpc.Status.StatusCode);
    }
}
