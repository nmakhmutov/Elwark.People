using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.ConfirmingEmail;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class ConfirmingEmailCommandTests
{
    private static readonly AccountId AccountId = new(200L);
    private static readonly DateTime Utc = new(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_PendingEmail_ReturnsTokenAndSendsMail()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId,
            time,
            pending: "pending@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .VerifyEmailAsync(AccountId, Arg.Is<MailAddress>(m => m.Address == "pending@test.com"), Arg.Any<CancellationToken>())
            .Returns(new ConfirmationResult("token-b64", "CODE9"));

        var notification = Substitute.For<INotificationSender>();

        var handler = new ConfirmingEmailCommandHandler(confirmation, notification, repo);

        var result = await handler.Handle(
            new ConfirmingEmailCommand(AccountId, new MailAddress("pending@test.com")),
            CancellationToken.None);

        Assert.Equal("token-b64", result.Token);
        await confirmation.Received(1).VerifyEmailAsync(
            AccountId,
            Arg.Is<MailAddress>(m => m.Address == "pending@test.com"),
            Arg.Any<CancellationToken>());
        await notification.Received(1).SendConfirmationAsync(
            Arg.Is<MailAddress>(m => m.Address == "pending@test.com"),
            "CODE9",
            Arg.Any<Language>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new ConfirmingEmailCommandHandler(
            Substitute.For<IConfirmationService>(),
            Substitute.For<INotificationSender>(),
            repo);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new ConfirmingEmailCommand(AccountId, new MailAddress("a@test.com")), CancellationToken.None));
    }
}
