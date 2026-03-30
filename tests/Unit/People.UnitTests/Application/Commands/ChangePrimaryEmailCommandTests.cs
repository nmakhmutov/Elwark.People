using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.ChangePrimaryEmail;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class ChangePrimaryEmailCommandTests
{
    private static readonly AccountId AccountId = new(400L);
    private static readonly DateTime Utc = new(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ConfirmedTarget_UpdatesPrimaryAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithTwoConfirmedEmails(
            AccountId,
            time,
            "a@test.com",
            "b@test.com");
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new ChangePrimaryEmailCommandHandler(repo);

        await handler.Handle(new ChangePrimaryEmailCommand(AccountId, new MailAddress("b@test.com")), CancellationToken.None);

        Assert.True(account.Emails.Single(e => e.Email == "b@test.com").IsPrimary);
        Assert.False(account.Emails.Single(e => e.Email == "a@test.com").IsPrimary);
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new ChangePrimaryEmailCommandHandler(repo);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new ChangePrimaryEmailCommand(AccountId, new MailAddress("b@test.com")), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TargetUnconfirmed_ThrowsNotConfirmed()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: "wait@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new ChangePrimaryEmailCommandHandler(repo);

        var ex = await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(new ChangePrimaryEmailCommand(AccountId, new MailAddress("wait@test.com")), CancellationToken.None));

        Assert.Equal(nameof(EmailException.NotConfirmed), ex.Code);
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
    }
}
