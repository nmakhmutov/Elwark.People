using System.Net.Mail;
using Grpc.Core;
using Mediator;
using NSubstitute;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using People.Grpc.People;
using Xunit;
using DomainLanguage = People.Domain.ValueObjects.Language;

namespace People.IntegrationTests.Grpc;

public sealed class PeopleServiceEmailFlowTests(PostgreSqlFixture postgres) : GrpcPeopleServiceTestBase(postgres)
{
    [Fact]
    public async Task SigningUpByEmail_ReturnsToken()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        Commands.Notification
            .SendConfirmationAsync(Arg.Any<MailAddress>(), Arg.Any<string>(), Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var reply = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningUpRequest
            {
                Email = GrpcProtoTestData.Email("grpc-flow-user@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SigningUpByEmail(req, ctx));

        Assert.False(string.IsNullOrEmpty(reply.Token));
    }

    [Fact]
    public async Task SignUpByEmail_WithValidTokenAndCode_ReturnsSignUpReply()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        string? capturedCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var signingUp = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningUpRequest
            {
                Email = GrpcProtoTestData.Email("signup-grpc@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SigningUpByEmail(req, ctx));

        Assert.NotNull(capturedCode);

        var signUp = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSignUpRequest
            {
                Token = signingUp.Token,
                Code = capturedCode!,
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SignUpByEmail(req, ctx));

        Assert.True(signUp.Id > 0);
        Assert.Equal("signup-grpc", signUp.FullName);
    }

    [Fact]
    public async Task SigningInByEmail_WithExistingEmail_ReturnsToken()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using (var seedScope = Commands.CreateScope())
        {
            _ = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("grpc-signin@example.com"),
                CancellationToken.None
            );
        }

        string? signInCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => signInCode = c),
                Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var signingIn = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningInRequest
            {
                Email = GrpcProtoTestData.Email("grpc-signin@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
            },
            static (s, req, ctx) => s.SigningInByEmail(req, ctx));

        Assert.False(string.IsNullOrEmpty(signingIn.Token));
        Assert.NotNull(signInCode);
    }

    [Fact]
    public async Task SignInByEmail_WithValidTokenAndCode_ReturnsSignInReply()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using (var seedScope = Commands.CreateScope())
        {
            _ = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("grpc-login@example.com"),
                CancellationToken.None
            );
        }

        string? signInCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => signInCode = c),
                Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var signingIn = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningInRequest
            {
                Email = GrpcProtoTestData.Email("grpc-login@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
            },
            static (s, req, ctx) => s.SigningInByEmail(req, ctx));

        var signIn = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSignInRequest
            {
                Token = signingIn.Token,
                Code = signInCode!,
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SignInByEmail(req, ctx));

        Assert.True(signIn.Id > 0);
        Assert.Equal("grpc-login-nick", signIn.FullName);
    }

    [Fact]
    public async Task SignUpByEmail_InvalidToken_ThrowsRpcInvalidArgument()
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
                new EmailSignUpRequest
                {
                    Token = "not-a-real-signup-token",
                    Code = "123456",
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignUpByEmail(req, ctx)));

        Assert.Equal(StatusCode.InvalidArgument, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task SignUpByEmail_WrongCode_ThrowsRpcInvalidArgument()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        string? capturedCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var signingUp = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningUpRequest
            {
                Email = GrpcProtoTestData.Email("wrong-code@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
                Metadata = GrpcProtoTestData.TestMetadata()
            },
            static (s, req, ctx) => s.SigningUpByEmail(req, ctx));

        Assert.NotNull(capturedCode);

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new EmailSignUpRequest
                {
                    Token = signingUp.Token,
                    Code = "000000",
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignUpByEmail(req, ctx)));

        Assert.Equal(StatusCode.InvalidArgument, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task SignInByEmail_InvalidToken_ThrowsRpcInvalidArgument()
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
                new EmailSignInRequest
                {
                    Token = "not-a-real-signin-token",
                    Code = "123456",
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignInByEmail(req, ctx)));

        Assert.Equal(StatusCode.InvalidArgument, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task SignInByEmail_WrongCode_ThrowsRpcInvalidArgument()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using (var seedScope = Commands.CreateScope())
        {
            _ = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("grpc-bad-signin@example.com"),
                CancellationToken.None
            );
        }

        Commands.Notification
            .SendConfirmationAsync(Arg.Any<MailAddress>(), Arg.Any<string>(), Arg.Any<DomainLanguage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var service = CreatePeopleService(mediator);
        var interceptor = CreateInterceptor();

        var signingIn = await InterceptUnaryAsync(
            interceptor,
            service,
            new EmailSigningInRequest
            {
                Email = GrpcProtoTestData.Email("grpc-bad-signin@example.com"),
                Language = GrpcProtoTestData.EnLanguage(),
            },
            static (s, req, ctx) => s.SigningInByEmail(req, ctx));

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            InterceptUnaryAsync(
                interceptor,
                service,
                new EmailSignInRequest
                {
                    Token = signingIn.Token,
                    Code = "wrong-code",
                    Metadata = GrpcProtoTestData.TestMetadata()
                },
                static (s, req, ctx) => s.SignInByEmail(req, ctx)));

        Assert.Equal(StatusCode.InvalidArgument, rpc.Status.StatusCode);
    }
}
