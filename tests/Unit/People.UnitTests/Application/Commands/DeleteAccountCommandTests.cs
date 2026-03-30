using NSubstitute;
using People.Api.Application.Commands.DeleteAccount;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class DeleteAccountCommandTests
{
    private static readonly AccountId AccountId = new(502L);
    private static readonly DateTime Utc = new(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_AccountExists_DeletesRaisesDomainEventAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new DeleteAccountCommandHandler(repo);

        await handler.Handle(new DeleteAccountCommand(AccountId), CancellationToken.None);

        repo.Received(1).Delete(account);
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
        Assert.Contains(account.DomainEvents, e => e is AccountDeletedDomainEvent d && d.Id == AccountId);
    }

    [Fact]
    public async Task Handle_AccountMissing_CompletesWithoutDeleteOrSave()
    {
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new DeleteAccountCommandHandler(repo);

        await handler.Handle(new DeleteAccountCommand(AccountId), CancellationToken.None);

        repo.DidNotReceive().Delete(Arg.Any<Account>());
        await uow.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
