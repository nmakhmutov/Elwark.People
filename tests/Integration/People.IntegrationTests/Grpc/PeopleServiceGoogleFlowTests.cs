using System.Net;
using System.Net.Mail;
using Grpc.Core;
using Mediator;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Api.Application.Commands.SignUpByGoogle;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;

namespace People.IntegrationTests.Grpc;

public sealed class PeopleServiceGoogleFlowTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task SignUpByGoogle_ReturnsSignUpReply()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var googleEmail = new MailAddress("grpc-google-signup@example.com");
        Commands.Google.GetAsync("grpc-google-signup-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "grpc-google-sub-1",
                googleEmail,
                true,
                "Go",
                "User",
                null,
                null));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalSignUpRequest
            {
                AccessToken = "grpc-google-signup-token",
                Language = GrpcProtoTestData.EnLanguage(),
                Ip = GrpcProtoTestData.LoopbackIp(),
                UserAgent = GrpcProtoTestData.TestUserAgent()
            },
            static (s, req, ctx) => s.SignUpByGoogle(req, ctx));

        Assert.True(reply.Id > 0);
        Assert.False(string.IsNullOrEmpty(reply.FullName));
    }

    [Fact]
    public async Task SignInByGoogle_ReturnsSignInReply_WhenLinkExists()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var googleEmail = new MailAddress("grpc-google-signin@example.com");
        Commands.Google.GetAsync("grpc-google-signin-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "grpc-google-sub-signin",
                googleEmail,
                true,
                "In",
                "User",
                null,
                null));

        using (var seedScope = Commands.CreateScope())
        {
            var mediatorSeed = seedScope.ServiceProvider.GetRequiredService<IMediator>();
            await mediatorSeed.Send(
                new SignUpByGoogleCommand("grpc-google-signin-token", Domain.ValueObjects.Language.Parse("en"), IPAddress.Loopback, null),
                CancellationToken.None);
        }

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalSignInRequest
            {
                AccessToken = "grpc-google-signin-token",
                Ip = GrpcProtoTestData.LoopbackIp(),
                UserAgent = GrpcProtoTestData.TestUserAgent()
            },
            static (s, req, ctx) => s.SignInByGoogle(req, ctx));

        Assert.True(reply.Id > 0);
        Assert.False(string.IsNullOrEmpty(reply.FullName));
    }

    [Fact]
    public async Task AppendGoogle_ReturnsEmpty_WhenAccountSeeded()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("append-google-base@example.com"),
                "append-base",
                CancellationToken.None);
        }

        Commands.Google.GetAsync("grpc-google-append-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "grpc-google-append-sub",
                new MailAddress("append-google-extra@example.com"),
                true,
                "App",
                "End",
                null,
                null));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var empty = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalAppendRequest
            {
                Id = id,
                AccessToken = "grpc-google-append-token"
            },
            static (s, req, ctx) => s.AppendGoogle(req, ctx));

        Assert.NotNull(empty);
        Assert.Equal(0, empty.CalculateSize());
    }

    [Fact]
    public async Task SignUpByGoogle_InvalidAccessToken_ThrowsRpcInternal_WhenGoogleThrows()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        Commands.Google
            .GetAsync("bad-google-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("invalid token"));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new ExternalSignUpRequest
                {
                    AccessToken = "bad-google-token",
                    Language = GrpcProtoTestData.EnLanguage(),
                    Ip = GrpcProtoTestData.LoopbackIp(),
                    UserAgent = GrpcProtoTestData.TestUserAgent()
                },
                static (s, req, ctx) => s.SignUpByGoogle(req, ctx)));

        Assert.Equal(StatusCode.Internal, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task SignUpByGoogle_SecondSignUpWithSameGoogleIdentity_ThrowsRpcAlreadyExists()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var googleEmail1 = new MailAddress("grpc-google-dup1@example.com");
        var googleEmail2 = new MailAddress("grpc-google-dup2@example.com");
        Commands.Google.GetAsync("grpc-google-dup-token-a", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "grpc-google-dup-sub",
                googleEmail1,
                true,
                "D",
                "One",
                null,
                null));
        Commands.Google.GetAsync("grpc-google-dup-token-b", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "grpc-google-dup-sub",
                googleEmail2,
                true,
                "D",
                "Two",
                null,
                null));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        _ = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalSignUpRequest
            {
                AccessToken = "grpc-google-dup-token-a",
                Language = GrpcProtoTestData.EnLanguage(),
                Ip = GrpcProtoTestData.LoopbackIp(),
                UserAgent = GrpcProtoTestData.TestUserAgent()
            },
            static (s, req, ctx) => s.SignUpByGoogle(req, ctx));

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new ExternalSignUpRequest
                {
                    AccessToken = "grpc-google-dup-token-b",
                    Language = GrpcProtoTestData.EnLanguage(),
                    Ip = GrpcProtoTestData.LoopbackIp(),
                    UserAgent = GrpcProtoTestData.TestUserAgent()
                },
                static (s, req, ctx) => s.SignUpByGoogle(req, ctx)));

        Assert.Equal(StatusCode.AlreadyExists, rpc.Status.StatusCode);
    }
}
