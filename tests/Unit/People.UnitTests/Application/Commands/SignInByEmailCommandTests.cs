using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Application.Commands.SignInByEmail;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class SignInByEmailCommandTests
{
    private static readonly AccountId TestAccountId = new(302L);

    [Fact]
    public async Task Handle_ValidConfirmation_ReturnsSignInResult()
    {
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(TestAccountId,
            TimeProvider.System, "user@example.com");

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation.SignInAsync("tok", "9999", Arg.Any<CancellationToken>()).Returns(TestAccountId);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new SignInByEmailCommandHandler(confirmation, repo, TimeProvider.System);
        var result = await handler.Handle(new SignInByEmailCommand("tok", "9999"), CancellationToken.None);

        Assert.Equal(TestAccountId, result.Id);
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        await confirmation.Received(1).DeleteAsync(TestAccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmationVerificationFails_Throws()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation.SignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.UnitOfWork.Returns(uow);

        var handler = new SignInByEmailCommandHandler(confirmation, repo, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new SignInByEmailCommand("x", "y"), CancellationToken.None).AsTask());

        await repo.DidNotReceive().GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountException()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation.SignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestAccountId);

        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.GetAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new SignInByEmailCommandHandler(confirmation, repo, TimeProvider.System);

        await Assert.ThrowsAsync<AccountException>(() =>
            handler.Handle(new SignInByEmailCommand("tok", "code"), CancellationToken.None).AsTask());
    }
}
