using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.DeleteEmail;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class DeleteEmailCommandTests
{
    private static readonly AccountId AccountId = new(500L);
    private static readonly DateTime Utc = new(2026, 5, 5, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_Secondary_RemovesAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithTwoConfirmedEmails(
            AccountId,
            time,
            "keep@test.com",
            "drop@test.com");
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new DeleteEmailCommandHandler(repo, time);

        await handler.Handle(new DeleteEmailCommand(AccountId, new MailAddress("drop@test.com")), CancellationToken.None);

        Assert.DoesNotContain(account.Emails, e => e.Email == "drop@test.com");
        Assert.Single(account.Emails);
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Primary_ThrowsCannotRemove()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time, "only@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new DeleteEmailCommandHandler(repo, time);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new DeleteEmailCommand(AccountId, new MailAddress("only@test.com")), CancellationToken.None));

        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new DeleteEmailCommandHandler(repo, TimeProvider.System);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new DeleteEmailCommand(AccountId, new MailAddress("x@test.com")), CancellationToken.None));
    }
}
