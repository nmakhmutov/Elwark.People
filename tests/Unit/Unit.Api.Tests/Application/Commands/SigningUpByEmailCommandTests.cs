using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SigningUpByEmail;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SigningUpByEmailCommandTests
{
    private static readonly DateTime Utc = new(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Timezone Timezone = Timezone.Utc;

    [Fact]
    public async Task Handle_NewEmail_CreatesAccountReturnsTokenAndSendsConfirmation()
    {
        var email = new MailAddress("new@example.com");
        var locale = Locale.Parse("en");
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1, 2]);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>()).Returns((EmailSignupState?)null);
        repo.AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Account>());

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        confirmation
            .IssueAsync(Arg.Any<AccountId>(), ConfirmationType.EmailSignUp, Arg.Any<CancellationToken>())
            .Returns(new ConfirmationChallenge(Guid.NewGuid(), "tok-new", "CODE1"));

        var notification = Substitute.For<INotificationSender>();

        var handler = new SigningUpByEmailCommandHandler(
            confirmation,
            hasher,
            notification,
            repo,
            time);

        var token = await handler.Handle(
            new SigningUpByEmailCommand(email, locale, Timezone, IPAddress.Loopback),
            CancellationToken.None
        );

        Assert.Equal("tok-new", token);
        await repo.Received(1).GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>());
        await repo.Received(1).AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.Received(1)
            .IssueAsync(Arg.Any<AccountId>(), ConfirmationType.EmailSignUp, Arg.Any<CancellationToken>());
        await notification.Received(1)
            .SendConfirmationAsync(
                Arg.Is<MailAddress>(m => m.Address == email.Address),
                "CODE1",
                locale,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_UnconfirmedEmailExists_ResendsConfirmationWithoutCreatingAccount()
    {
        var email = new MailAddress("pending@example.com");
        var locale = Locale.Parse("en");
        var existingId = new AccountId(42L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var hasher = Substitute.For<IIpHasher>();

        var repo = Substitute.For<IAccountRepository>();
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>())
            .Returns(new EmailSignupState(existingId, email, IsConfirmed: false));

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        confirmation
            .IssueAsync(existingId, ConfirmationType.EmailSignUp, Arg.Any<CancellationToken>())
            .Returns(new ConfirmationChallenge(Guid.NewGuid(), "tok-re", "CODE2"));

        var notification = Substitute.For<INotificationSender>();

        var handler = new SigningUpByEmailCommandHandler(
            confirmation,
            hasher,
            notification,
            repo,
            time);

        var token = await handler.Handle(
            new SigningUpByEmailCommand(email, locale, Timezone, IPAddress.Loopback),
            CancellationToken.None);

        Assert.Equal("tok-re", token);
        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await confirmation.Received(1)
            .IssueAsync(existingId, ConfirmationType.EmailSignUp, Arg.Any<CancellationToken>());
        await notification.Received(1)
            .SendConfirmationAsync(
                Arg.Is<MailAddress>(m => m.Address == email.Address),
                "CODE2",
                locale,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEmailExists_ThrowsAlreadyCreated()
    {
        var email = new MailAddress("taken@example.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>())
            .Returns(new EmailSignupState(new AccountId(9L), email, IsConfirmed: true));

        var handler = new SigningUpByEmailCommandHandler(
            Substitute.For<IConfirmationChallengeService>(),
            Substitute.For<IIpHasher>(),
            Substitute.For<INotificationSender>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(
                new SigningUpByEmailCommand(email, Locale.Parse("en"), Timezone, IPAddress.Loopback),
                CancellationToken.None
            ));

        Assert.Equal(nameof(EmailException.AlreadyCreated), ex.Code);
        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }
}
