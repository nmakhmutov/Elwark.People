using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SignUpByMicrosoft;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SignUpByMicrosoftCommandTests
{
    private static readonly DateTime Utc = new(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);

    private static MicrosoftAccount ValidMicrosoft(string identity = "ms-id-1", string email = "m@example.com") =>
        new(identity, new MailAddress(email), "Sam", "Smith");

    [Fact]
    public async Task Handle_ValidToken_CreatesAccountWithMicrosoftIdentityAndReturnsSignUpResult()
    {
        var language = Language.Parse("en");
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([3]);

        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync("ms-token", Arg.Any<CancellationToken>()).Returns(ValidMicrosoft());

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(ExternalService.Microsoft, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Account? added = null;
        repo.AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                added = callInfo.Arg<Account>();
                return callInfo.Arg<Account>();
            });

        var handler = new SignUpByMicrosoftCommandHandler(microsoft, hasher, repo, time);

        var result = await handler.Handle(
            new SignUpByMicrosoftCommand("ms-token", language, IPAddress.Loopback),
            CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("Sam", added!.Name.FirstName);
        Assert.Equal("Smith", added.Name.LastName);
        Assert.Equal(added.Name.FullName(), result.FullName);
        await microsoft.Received(1).GetAsync("ms-token", Arg.Any<CancellationToken>());
        await repo.Received(1).AddAsync(
            Arg.Is<Account>(a =>
                a.Emails.Any(e => e.Email == "m@example.com" && e.IsConfirmed)
                && a.Externals.Any(e => e.Type == ExternalService.Microsoft && e.Identity == "ms-id-1")),
            Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MicrosoftApiThrows_PropagatesException()
    {
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MicrosoftAccount>(new HttpRequestException("unauthorized")));

        var repo = Substitute.For<IAccountRepository>();

        var handler = new SignUpByMicrosoftCommandHandler(
            microsoft,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await handler.Handle(
                new SignUpByMicrosoftCommand("x", Language.Parse("en"), IPAddress.Loopback),
                CancellationToken.None));

        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ThrowsAlreadyCreated()
    {
        var mail = new MailAddress("m@example.com");
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValidMicrosoft(email: mail.Address));

        var repo = Substitute.For<IAccountRepository>();
        repo.IsExistsAsync(Arg.Is<MailAddress>(m => m.Address == mail.Address), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new SignUpByMicrosoftCommandHandler(
            microsoft,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(
                new SignUpByMicrosoftCommand("t", Language.Parse("en"), IPAddress.Loopback),
                CancellationToken.None));

        Assert.Equal(nameof(EmailException.AlreadyCreated), ex.Code);
    }

    [Fact]
    public async Task Handle_MicrosoftIdentityAlreadyLinked_ThrowsExternalAlreadyCreated()
    {
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidMicrosoft());

        var repo = Substitute.For<IAccountRepository>();
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(ExternalService.Microsoft, "ms-id-1", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new SignUpByMicrosoftCommandHandler(
            microsoft,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(
                new SignUpByMicrosoftCommand("t", Language.Parse("en"), IPAddress.Loopback),
                CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.AlreadyCreated), ex.Code);
        Assert.Equal(ExternalService.Microsoft, ex.Service);
    }
}
