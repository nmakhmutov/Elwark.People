using System.Globalization;
using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SignUpByGoogle;
using People.Application.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SignUpByGoogleCommandTests
{
    private static readonly DateTime Utc = new(2026, 6, 2, 11, 0, 0, DateTimeKind.Utc);
    private static readonly Timezone Timezone = Timezone.Utc;

    private static GoogleAccount ValidGoogle(string identity = "gid-1", string email = "g@example.com")
    {
        var picture = new Uri("https://lh3.googleusercontent.com/a/photo.jpg");
        return new GoogleAccount(
            identity,
            new MailAddress(email),
            isEmailVerified: true,
            firstName: "Ann",
            lastName: "Doe",
            picture,
            locale: new CultureInfo("en"));
    }

    [Fact]
    public async Task Handle_ValidToken_CreatesAccountWithPictureConfirmedEmailAndReturnsSignUpResult()
    {
        var locale = Locale.Parse("en");
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([9]);

        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync("access-token", Arg.Any<CancellationToken>()).Returns(ValidGoogle());

        Account? added = null;
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(ExternalService.Google, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                added = callInfo.Arg<Account>();
                return callInfo.Arg<Account>();
            });

        var handler = new SignUpByGoogleCommandHandler(google, hasher, repo, time);

        var result = await handler.Handle(
            new SignUpByGoogleCommand("access-token", locale, Timezone, IPAddress.Loopback),
            CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(Picture.Parse("https://lh3.googleusercontent.com/a/photo.jpg"), added!.Picture);
        Assert.Equal(added.Name.FullName(), result.FullName);
        Assert.Equal(added.Id, result.Id);
        Assert.Contains(added.Emails, e => e.Email == "g@example.com" && e.IsConfirmed);
        Assert.Contains(added.Externals, e => e.Type == ExternalService.Google && e.Identity == "gid-1");
        await google.Received(1).GetAsync("access-token", Arg.Any<CancellationToken>());
        await repo.Received(1).AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GoogleApiThrows_PropagatesException()
    {
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GoogleAccount>(new InvalidOperationException("Google email not found")));

        var repo = Substitute.For<IAccountRepository>();

        var handler = new SignUpByGoogleCommandHandler(
            google,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(
                new SignUpByGoogleCommand("bad", Locale.Parse("en"), Timezone, IPAddress.Loopback),
                CancellationToken.None));

        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ThrowsAlreadyCreated()
    {
        var mail = new MailAddress("g@example.com");
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidGoogle(email: mail.Address));

        var repo = Substitute.For<IAccountRepository>();
        repo.IsExistsAsync(Arg.Is<MailAddress>(m => m.Address == mail.Address), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new SignUpByGoogleCommandHandler(
            google,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(
                new SignUpByGoogleCommand("t", Locale.Parse("en"), Timezone, IPAddress.Loopback),
                CancellationToken.None));

        Assert.Equal(nameof(EmailException.AlreadyCreated), ex.Code);
        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GoogleIdentityAlreadyLinked_ThrowsExternalAlreadyCreated()
    {
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidGoogle());

        var repo = Substitute.For<IAccountRepository>();
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(ExternalService.Google, "gid-1", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new SignUpByGoogleCommandHandler(
            google,
            Substitute.For<IIpHasher>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(
                new SignUpByGoogleCommand("t", Locale.Parse("en"), Timezone, IPAddress.Loopback),
                CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.AlreadyCreated), ex.Code);
        Assert.Equal(ExternalService.Google, ex.Service);
        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }
}
