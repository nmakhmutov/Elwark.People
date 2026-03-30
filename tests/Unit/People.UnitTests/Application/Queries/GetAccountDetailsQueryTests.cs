using NSubstitute;
using People.Api.Application.Queries.GetAccountDetails;
using People.UnitTests.Application.Commands;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using Xunit;

namespace People.UnitTests.Application.Queries;

public sealed class GetAccountDetailsQueryTests
{
    private static readonly AccountId AccountId = new(801L);
    private static readonly DateTime Utc = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_AccountFound_ReturnsAccountFromRepository()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new GetAccountDetailsQueryHandler(repo);

        var result = await handler.Handle(new GetAccountDetailsQuery(AccountId), CancellationToken.None);

        Assert.Same(account, result);
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new GetAccountDetailsQueryHandler(repo);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new GetAccountDetailsQuery(AccountId), CancellationToken.None));

        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
    }
}
