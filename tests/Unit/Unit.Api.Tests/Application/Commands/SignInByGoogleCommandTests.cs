using System.Globalization;
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SignInByGoogle;
using People.Application.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SignInByGoogleCommandTests
{
    private static readonly DateTime Utc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static GoogleAccount GoogleProfile(string identity = "gid-99") =>
        new(
            identity,
            new MailAddress("g@example.com"),
            isEmailVerified: true,
            firstName: "G",
            lastName: "User",
            picture: null,
            locale: new CultureInfo("en"));

    [Fact]
    public async Task Handle_LinkedGoogleIdentity_ReturnsSignInResult()
    {
        var accountId = new AccountId(401L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);

        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync("access", Arg.Any<CancellationToken>()).Returns(GoogleProfile());

        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Google, "gid-99", Arg.Any<CancellationToken>()).Returns(account);
        repo.UnitOfWork.Returns(uow);

        var handler = new SignInByGoogleCommandHandler(repo, google, time);

        var result = await handler.Handle(new SignInByGoogleCommand("access"), CancellationToken.None);

        Assert.Equal(accountId, result.Id);
        await google.Received(1).GetAsync("access", Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(ExternalService.Google, "gid-99", Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GoogleIdentityNotLinked_ThrowsExternalNotFound()
    {
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(GoogleProfile("orphan"));

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Google, "orphan", Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new SignInByGoogleCommandHandler(repo, google, TimeProvider.System);

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(new SignInByGoogleCommand("t"), CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.NotFound), ex.Code);
    }
}
