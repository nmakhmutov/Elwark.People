using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.ConfirmingEmail;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class ConfirmingEmailCommandTests
{
    private static readonly AccountId AccountId = new(200L);
    private static readonly DateTime Utc = new(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_PendingEmail_ReturnsToken()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId,
            time,
            pending: "pending@test.com");
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var challenge = new ConfirmationChallenge(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "challenge-token",
            "ABC123");
        var tokens = Substitute.For<IEmailVerificationTokenService>();
        confirmation
            .IssueAsync(AccountId, ConfirmationType.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(challenge);
        tokens
            .CreateToken(challenge.Id, Arg.Is<MailAddress>(m => m.Address == "pending@test.com"))
            .Returns("encrypted-token");

        var handler = new ConfirmingEmailCommandHandler(confirmation, tokens, repo, time);

        var result = await handler.Handle(
            new ConfirmingEmailCommand(AccountId, new MailAddress("pending@test.com")),
            CancellationToken.None);

        Assert.Equal("encrypted-token", result.Token);
        await confirmation.Received(1).IssueAsync(AccountId, ConfirmationType.EmailConfirmation, Arg.Any<CancellationToken>());
        tokens.Received(1).CreateToken(
            challenge.Id,
            Arg.Is<MailAddress>(m => m.Address == "pending@test.com"));
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new ConfirmingEmailCommandHandler(
            Substitute.For<IConfirmationChallengeService>(),
            Substitute.For<IEmailVerificationTokenService>(),
            repo,
            time);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new ConfirmingEmailCommand(AccountId, new MailAddress("a@test.com")), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IssueActiveChallenge_ThrowsAlreadySent()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: "pending@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        confirmation
            .IssueAsync(AccountId, ConfirmationType.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ConfirmationChallenge>(ConfirmationException.AlreadySent()));

        var handler = new ConfirmingEmailCommandHandler(
            confirmation,
            Substitute.For<IEmailVerificationTokenService>(),
            repo,
            time);

        await Assert.ThrowsAsync<ConfirmationException>(async () =>
            await handler.Handle(
                new ConfirmingEmailCommand(AccountId, new MailAddress("pending@test.com")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedEmail_ThrowsEmailException()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time, "confirmed@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new ConfirmingEmailCommandHandler(
            Substitute.For<IConfirmationChallengeService>(),
            Substitute.For<IEmailVerificationTokenService>(),
            repo,
            time);

        await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(
                new ConfirmingEmailCommand(AccountId, new MailAddress("confirmed@test.com")),
                CancellationToken.None));
    }
}
