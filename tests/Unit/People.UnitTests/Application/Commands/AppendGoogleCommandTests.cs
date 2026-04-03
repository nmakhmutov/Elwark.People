using System.Globalization;
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.AppendGoogle;
using People.Application.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class AppendGoogleCommandTests
{
    private static readonly AccountId AccountId = new(503L);
    private static readonly DateTime Utc = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    private static GoogleAccount Profile(string identity, string email) =>
        new(
            identity,
            new MailAddress(email),
            isEmailVerified: true,
            firstName: "G",
            lastName: "Extra",
            picture: null,
            locale: new CultureInfo("en"));

    [Fact]
    public async Task Handle_AppendsGoogleIdentityAndEmailWhenNotAlreadyRegistered()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time, "local@example.com");

        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync("tok", Arg.Any<CancellationToken>()).Returns(Profile("gid-append", "gextra@example.com"));

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
        repo.IsExistsAsync(ExternalService.Google, "gid-append", Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(Arg.Is<MailAddress>(m => m.Address == "gextra@example.com"), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new AppendGoogleCommandHandler(google, repo, time);

        await handler.Handle(new AppendGoogleCommand(AccountId, "tok"), CancellationToken.None);

        Assert.Contains(account.Externals, e => e.Type == ExternalService.Google && e.Identity == "gid-append");
        Assert.Contains(account.Emails, e => e.Email == "gextra@example.com");
        await google.Received(1).GetAsync("tok", Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new AppendGoogleCommandHandler(
            Substitute.For<IGoogleApiService>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new AppendGoogleCommand(AccountId, "t"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GoogleIdentityAlreadyRegisteredGlobally_SkipsAddGoogle()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);

        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Profile("gid-taken", "other@example.com"));

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
        repo.IsExistsAsync(ExternalService.Google, "gid-taken", Arg.Any<CancellationToken>()).Returns(true);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new AppendGoogleCommandHandler(google, repo, time);

        await handler.Handle(new AppendGoogleCommand(AccountId, "t"), CancellationToken.None);

        Assert.DoesNotContain(account.Externals, e => e.Identity == "gid-taken");
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
