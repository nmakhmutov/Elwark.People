using NSubstitute;
using People.Application.Commands.SignUpByEmail;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SignUpByEmailCommandTests
{
    private static readonly AccountId AccountId = new(55L);
    private static readonly DateTime Utc = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ValidConfirmation_ConfirmsPrimaryReturnsSignUpResultAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedPrimary(AccountId, time, "finish@example.com");

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .SignUpAsync("token-b64", "123456", Arg.Any<CancellationToken>())
            .Returns(AccountId);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new SignUpByEmailCommandHandler(confirmation, repo, time);

        var result = await handler.Handle(
            new SignUpByEmailCommand("token-b64", "123456"),
            CancellationToken.None);

        Assert.Equal(AccountId, result.Id);
        Assert.Equal(account.Name.FullName(), result.FullName);
        Assert.True(account.Emails.Single(e => e.Email == "finish@example.com").IsConfirmed);
        await confirmation.Received(1).SignUpAsync("token-b64", "123456", Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmationVerificationFails_PropagatesException()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .SignUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AccountId>(new InvalidOperationException("bad code")));

        var repo = Substitute.For<IAccountRepository>();

        var handler = new SignUpByEmailCommandHandler(confirmation, repo, EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(
                new SignUpByEmailCommand("t", "wrong"),
                CancellationToken.None));

        await repo.DidNotReceive().GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissingAfterConfirmation_ThrowsNotFound()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .SignUpAsync("ok", "ok", Arg.Any<CancellationToken>())
            .Returns(AccountId);

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new SignUpByEmailCommandHandler(
            confirmation,
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new SignUpByEmailCommand("ok", "ok"), CancellationToken.None));
    }
}
