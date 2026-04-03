using NSubstitute;
using People.Application.Commands.DeleteMicrosoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class DeleteMicrosoftCommandTests
{
    private static readonly AccountId AccountId = new(506L);
    private static readonly DateTime Utc = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_RemovesMicrosoftIdentityAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        account.AddMicrosoft("ms-remove", "M", "One", time);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new DeleteMicrosoftCommandHandler(repo, time);

        await handler.Handle(new DeleteMicrosoftCommand(AccountId, "ms-remove"), CancellationToken.None);

        Assert.DoesNotContain(account.Externals, e => e.Type == ExternalService.Microsoft && e.Identity == "ms-remove");
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new DeleteMicrosoftCommandHandler(repo, TimeProvider.System);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new DeleteMicrosoftCommand(AccountId, "any"), CancellationToken.None));
    }
}
