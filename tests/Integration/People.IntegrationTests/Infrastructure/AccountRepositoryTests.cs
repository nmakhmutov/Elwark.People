using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Infrastructure.Repositories;
using Xunit;

namespace People.IntegrationTests.Infrastructure;

[Collection(nameof(PostgresCollection))]
public sealed class AccountRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task GetAsync_ReturnsAccountWithEmailsAndExternalsLoaded()
    {
        var mediator = new NoOpMediator();
        await using var write = fixture.CreateContext(mediator);
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var hasher = AccountTestFactory.FakeIpHasher();
        var time = AccountTestFactory.FixedUtc(new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc));
        var account = AccountTestFactory.CreateNewAccount(hasher, time, "with-graph");
        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        account.AddEmail(new MailAddress("graph@example.com"), true, time);
        account.AddGoogle("google-sub", "G", "User", time);
        await write.SaveEntitiesAsync(CancellationToken.None);

        await using var read = fixture.CreateContext(new NoOpMediator());
        var repo = new AccountRepository(read);
        var loaded = await repo.GetAsync(account.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Emails);
        Assert.Equal("graph@example.com", loaded.Emails.First().Email);
        Assert.Single(loaded.Externals);
        Assert.Equal(ExternalService.Google, loaded.Externals.First().Type);
        Assert.Equal("google-sub", loaded.Externals.First().Identity);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_ForUnknownId()
    {
        await using var read = fixture.CreateContext(new NoOpMediator());
        var repo = new AccountRepository(read);

        var loaded = await repo.GetAsync(new AccountId(long.MaxValue), CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task AddAsync_PersistsAccount_WithGeneratedId()
    {
        var mediator = new NoOpMediator();
        await using var write = fixture.CreateContext(mediator);
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var repo = new AccountRepository(write);
        var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), TimeProvider.System, "new-id");
        Assert.True(account.Id == default);

        await repo.AddAsync(account, CancellationToken.None);
        await write.SaveEntitiesAsync(CancellationToken.None);

        Assert.False(account.Id == default);

        await using var read = fixture.CreateContext(new NoOpMediator());
        var loaded = await read.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == account.Id);
        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task Delete_RemovesAccount_AndCascadeDeletesEmailsAndConnections()
    {
        var mediator = new NoOpMediator();
        await using var write = fixture.CreateContext(mediator);
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var time = AccountTestFactory.FixedUtc(new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc));
        var repo = new AccountRepository(write);
        var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), time, "del");
        await repo.AddAsync(account, CancellationToken.None);
        await write.SaveEntitiesAsync(CancellationToken.None);

        account.AddEmail(new MailAddress("del@example.com"), true, time);
        account.AddMicrosoft("ms-id", "M", "S", time);
        await write.SaveEntitiesAsync(CancellationToken.None);

        var id = account.Id;

        var tracked = await repo.GetAsync(id, CancellationToken.None);
        Assert.NotNull(tracked);
        repo.Delete(tracked);
        await write.SaveEntitiesAsync(CancellationToken.None);

        await using var verify = fixture.CreateContext(new NoOpMediator());
        Assert.Null(await verify.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id));
        Assert.Equal(0, await verify.Emails.CountAsync(e => e.AccountId == id));
        Assert.Equal(0, await verify.Connections.CountAsync());
    }
}
