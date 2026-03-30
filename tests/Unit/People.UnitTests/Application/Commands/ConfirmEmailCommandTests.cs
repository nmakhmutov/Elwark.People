using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.ConfirmEmail;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class ConfirmEmailCommandTests
{
    private static readonly AccountId AccountId = new(300L);
    private static readonly DateTime Utc = new(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ValidToken_ReturnsConfirmedEmailAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var pendingAddr = new MailAddress("pending@test.com");
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: pendingAddr.Address);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .VerifyEmailAsync("tok", "123456", Arg.Any<CancellationToken>())
            .Returns(new EmailConfirmation(AccountId, pendingAddr));

        var handler = new ConfirmEmailCommandHandler(confirmation, time, repo);

        var result = await handler.Handle(new ConfirmEmailCommand("tok", "123456"), CancellationToken.None);

        Assert.Equal(pendingAddr.Address, result.Email);
        Assert.True(result.IsConfirmed);
        await confirmation.Received(1).VerifyEmailAsync("tok", "123456", Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VerificationFails_PropagatesException()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .VerifyEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EmailConfirmation>(new InvalidOperationException("bad token")));

        var handler = new ConfirmEmailCommandHandler(confirmation, time, repo);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new ConfirmEmailCommand("bad", "bad"), CancellationToken.None));

        await repo.DidNotReceive().GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_CallsSaveEntitiesAsync()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var pendingAddr = new MailAddress("p2@test.com");
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: pendingAddr.Address);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .VerifyEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailConfirmation(AccountId, pendingAddr));

        var handler = new ConfirmEmailCommandHandler(confirmation, time, repo);

        await handler.Handle(new ConfirmEmailCommand("t", "c"), CancellationToken.None);

        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
