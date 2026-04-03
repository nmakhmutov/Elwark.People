using NSubstitute;
using People.Application.Commands.DeleteGoogle;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class DeleteGoogleCommandTests
{
    private static readonly AccountId AccountId = new(505L);
    private static readonly DateTime Utc = new(2026, 7, 4, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_RemovesGoogleIdentityAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        account.AddGoogle("gid-remove", "G", "One", null, time);

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new DeleteGoogleCommandHandler(repo, time);

        await handler.Handle(new DeleteGoogleCommand(AccountId, "gid-remove"), CancellationToken.None);

        Assert.DoesNotContain(account.Externals, e => e.Type == ExternalService.Google && e.Identity == "gid-remove");
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new DeleteGoogleCommandHandler(repo, TimeProvider.System);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new DeleteGoogleCommand(AccountId, "any"), CancellationToken.None));
    }
}
