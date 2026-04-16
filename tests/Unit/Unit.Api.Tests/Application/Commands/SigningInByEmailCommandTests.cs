using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SigningInByEmail;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SigningInByEmailCommandTests
{
    private static readonly AccountId AccountId = new(301L);

    [Fact]
    public async Task Handle_ConfirmedEmail_CreatesSignInConfirmationReturnsTokenAndSendsNotification()
    {
        var email = new MailAddress("user@example.com");
        var locale = Locale.Parse("en");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>())
            .Returns(new EmailSignupState(AccountId, email, IsConfirmed: true));

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        confirmation
            .IssueAsync(AccountId, ConfirmationType.EmailSignIn, Arg.Any<CancellationToken>())
            .Returns(new ConfirmationChallenge(Guid.NewGuid(), "signin-token", "PIN42"));

        var notification = Substitute.For<INotificationSender>();

        var handler = new SigningInByEmailCommandHandler(confirmation, notification, repo);

        var token = await handler.Handle(new SigningInByEmailCommand(email, locale), CancellationToken.None);

        Assert.Equal("signin-token", token);
        await repo.Received(1).GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>());
        await confirmation.Received(1).IssueAsync(AccountId, ConfirmationType.EmailSignIn, Arg.Any<CancellationToken>());
        await notification.Received(1).SendConfirmationAsync(email, "PIN42", locale, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailNotFound_ThrowsNotFound()
    {
        var email = new MailAddress("missing@example.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>()).Returns((EmailSignupState?)null);

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var handler = new SigningInByEmailCommandHandler(
            confirmation,
            Substitute.For<INotificationSender>(),
            repo);

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(new SigningInByEmailCommand(email, Locale.Parse("en")), CancellationToken.None));

        Assert.Equal(nameof(EmailException.NotFound), ex.Code);
        await confirmation.DidNotReceive().IssueAsync(Arg.Any<AccountId>(), Arg.Any<ConfirmationType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ThrowsNotConfirmed()
    {
        var email = new MailAddress("pending@example.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetEmailSignupStateAsync(email, Arg.Any<CancellationToken>())
            .Returns(new EmailSignupState(AccountId, email, IsConfirmed: false));

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var notification = Substitute.For<INotificationSender>();

        var handler = new SigningInByEmailCommandHandler(confirmation, notification, repo);

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(new SigningInByEmailCommand(email, Locale.Parse("en")), CancellationToken.None));

        Assert.Equal(nameof(EmailException.NotConfirmed), ex.Code);
        await confirmation.DidNotReceive().IssueAsync(Arg.Any<AccountId>(), Arg.Any<ConfirmationType>(), Arg.Any<CancellationToken>());
        await notification.DidNotReceive()
            .SendConfirmationAsync(Arg.Any<MailAddress>(), Arg.Any<string>(), Arg.Any<Locale>(), Arg.Any<CancellationToken>());
    }
}
