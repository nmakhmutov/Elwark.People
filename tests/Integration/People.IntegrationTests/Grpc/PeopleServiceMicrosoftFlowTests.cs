using System.Net;
using System.Net.Mail;
using Grpc.Core;
using Mediator;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Application.Commands.SignUpByMicrosoft;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;

namespace People.IntegrationTests.Grpc;

public sealed class PeopleServiceMicrosoftFlowTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task SignUpByMicrosoft_ReturnsSignUpReply()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var msEmail = new MailAddress("grpc-ms-signup@example.com");
        Commands.Microsoft.GetAsync("grpc-ms-signup-token", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("grpc-ms-sub-1", msEmail, "Ms", "User"));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalSignUpRequest
            {
                AccessToken = "grpc-ms-signup-token",
                Language = GrpcProtoTestData.EnLanguage(),
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SignUpByMicrosoft(req, ctx));

        Assert.True(reply.Id > 0);
        Assert.False(string.IsNullOrEmpty(reply.FullName));
    }

    [Fact]
    public async Task SignInByMicrosoft_ReturnsSignInReply_WhenLinkExists()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var msEmail = new MailAddress("grpc-ms-signin@example.com");
        Commands.Microsoft.GetAsync("grpc-ms-signin-token", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("grpc-ms-sub-signin", msEmail, "In", "User"));

        using (var seedScope = Commands.CreateScope())
        {
            var mediatorSeed = seedScope.ServiceProvider.GetRequiredService<IMediator>();
            await mediatorSeed.Send(
                new SignUpByMicrosoftCommand("grpc-ms-signin-token", Domain.ValueObjects.Language.Parse("en"), IPAddress.Loopback),
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
                AccessToken = "grpc-ms-signin-token",
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SignInByMicrosoft(req, ctx));

        Assert.True(reply.Id > 0);
        Assert.False(string.IsNullOrEmpty(reply.FullName));
    }

    [Fact]
    public async Task AppendMicrosoft_ReturnsEmpty_WhenAccountSeeded()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("append-ms-base@example.com"),
                "append-ms-base",
                CancellationToken.None);
        }

        Commands.Microsoft.GetAsync("grpc-ms-append-token", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount(
                "grpc-ms-append-sub",
                new MailAddress("append-ms-extra@example.com"),
                "App",
                "End"));

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
                AccessToken = "grpc-ms-append-token"
            },
            static (s, req, ctx) => s.AppendMicrosoft(req, ctx));

        Assert.NotNull(empty);
        Assert.Equal(0, empty.CalculateSize());
    }

    [Fact]
    public async Task SignUpByMicrosoft_InvalidAccessToken_ThrowsRpcInternal_WhenMicrosoftThrows()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        Commands.Microsoft
            .GetAsync("bad-ms-token", Arg.Any<CancellationToken>())
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
                    AccessToken = "bad-ms-token",
                    Language = GrpcProtoTestData.EnLanguage(),
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignUpByMicrosoft(req, ctx)));

        Assert.Equal(StatusCode.Internal, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task SignUpByMicrosoft_SecondSignUpWithSameMicrosoftIdentity_ThrowsRpcAlreadyExists()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var email1 = new MailAddress("grpc-ms-dup1@example.com");
        var email2 = new MailAddress("grpc-ms-dup2@example.com");
        Commands.Microsoft.GetAsync("grpc-ms-dup-token-a", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("grpc-ms-dup-sub", email1, "D", "One"));
        Commands.Microsoft.GetAsync("grpc-ms-dup-token-b", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("grpc-ms-dup-sub", email2, "D", "Two"));

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        _ = await InterceptUnaryAsync(
            interceptor,
            service,
            new ExternalSignUpRequest
            {
                AccessToken = "grpc-ms-dup-token-a",
                Language = GrpcProtoTestData.EnLanguage(),
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SignUpByMicrosoft(req, ctx));

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new ExternalSignUpRequest
                {
                    AccessToken = "grpc-ms-dup-token-b",
                    Language = GrpcProtoTestData.EnLanguage(),
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignUpByMicrosoft(req, ctx)));

        Assert.Equal(StatusCode.AlreadyExists, rpc.Status.StatusCode);
    }
}
