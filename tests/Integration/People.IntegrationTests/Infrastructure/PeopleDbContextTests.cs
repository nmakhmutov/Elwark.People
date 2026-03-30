using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using Xunit;

namespace People.IntegrationTests.Infrastructure;

[Collection(nameof(PostgresCollection))]
public sealed class PeopleDbContextTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task SaveEntitiesAsync_PersistsAccount()
    {
        var mediator = new NoOpMediator();
        await using var write = fixture.CreateContext(mediator);
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher());
        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        await using var read = fixture.CreateContext(new NoOpMediator());
        var loaded = await read.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == account.Id);

        Assert.NotNull(loaded);
        Assert.Equal(account.Id, loaded.Id);
        Assert.Equal("integration", loaded.Name.Nickname);
    }

    [Fact]
    public async Task SaveEntitiesAsync_DispatchesDomainEvents_ViaMediatorPublish()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        mediator.Publish(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        await using var write = fixture.CreateContext(mediator);
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var account = Account.Create("evt", Language.Parse("en"), System.Net.IPAddress.Loopback, AccountTestFactory.FakeIpHasher());
        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        await mediator.Received(1).Publish(
            Arg.Is<AccountCreatedDomainEvent>(e => e.Account.Id == account.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveEntitiesAsync_SecondWriterWithStaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        var mediator = new NoOpMediator();
        await using (var setup = fixture.CreateContext(mediator))
        {
            await IntegrationDatabaseCleanup.DeleteAllAsync(setup);
            var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), "conc");
            setup.Accounts.Add(account);
            await setup.SaveEntitiesAsync(CancellationToken.None);
        }

        await using var ctx1 = fixture.CreateContext(new NoOpMediator());
        await using var ctx2 = fixture.CreateContext(new NoOpMediator());

        var id = await ctx1.Accounts.Select(a => a.Id).FirstAsync();
        var a1 = await ctx1.Accounts.FirstAsync(a => a.Id == id);
        var a2 = await ctx2.Accounts.FirstAsync(a => a.Id == id);

        a1.Update("first-win", null, null, true);
        await ctx1.SaveEntitiesAsync(CancellationToken.None);

        a2.Update("second-lose", null, null, true);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveEntitiesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Database_OnFreshContainer_MigrationsApplyAndSchemaIsQueryable()
    {
        await using var ctx = fixture.CreateContext(new NoOpMediator());

        var pending = await ctx.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);

        var applied = await ctx.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, m => m.Contains("Init", StringComparison.OrdinalIgnoreCase));

        Assert.True(await ctx.Database.CanConnectAsync());
        await ctx.Accounts.CountAsync();
    }
}
