using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.AppendEmail;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class AppendEmailCommandTests
{
    private static readonly AccountId AccountId = new(100L);
    private static readonly DateTime Utc = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_NewEmail_AppendsAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new AppendEmailCommandHandler(time, repo);
        var newAddr = new MailAddress("extra@test.com");

        var result = await handler.Handle(new AppendEmailCommand(AccountId, newAddr), CancellationToken.None);

        Assert.Equal(newAddr.Address, result.Email);
        await repo.Received(1).IsExistsAsync(newAddr, Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new AppendEmailCommandHandler(time, repo);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new AppendEmailCommand(AccountId, new MailAddress("x@test.com")), CancellationToken.None));

        await repo.Received(1).IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailGloballyTaken_ThrowsAlreadyCreated()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var taken = new MailAddress("taken@test.com");
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.IsExistsAsync(taken, Arg.Any<CancellationToken>()).Returns(true);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new AppendEmailCommandHandler(time, repo);

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(new AppendEmailCommand(AccountId, taken), CancellationToken.None));

        Assert.Equal(nameof(EmailException.AlreadyCreated), ex.Code);
        await repo.Received(1).IsExistsAsync(taken, Arg.Any<CancellationToken>());
        await repo.DidNotReceive().GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_CallsSaveEntitiesAsync()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new AppendEmailCommandHandler(time, repo);

        await handler.Handle(new AppendEmailCommand(AccountId, new MailAddress("new@test.com")), CancellationToken.None);

        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
