using System.Globalization;
using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using Xunit;

namespace Integration.Api.Tests.Infrastructure;

[Collection(nameof(PostgresCollection))]
public sealed class PeopleDbContextTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task SaveEntitiesAsync_PersistsAccount()
    {
        await using var write = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var account = AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), TimeProvider.System);
        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        await using var read = fixture.CreateContext();
        var loaded = await read.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == account.Id);

        Assert.NotNull(loaded);
        Assert.Equal(account.Id, loaded.Id);
        Assert.Equal(Nickname.Parse("integration"), loaded.Name.Nickname);
    }

    [Fact]
    public async Task SaveEntitiesAsync_PersistsOutboxMessage_WhenAccountCreated()
    {
        await using var write = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var account = Account.Create(
            Timezone.Utc,
            AccountTestFactory.EnLocale,
            System.Net.IPAddress.Loopback,
            AccountTestFactory.FakeIpHasher(),
            TimeProvider.System
        );

        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        Assert.Equal(1, await write.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task SaveEntitiesAsync_SecondWriterWithStaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        await using (var setup = fixture.CreateContext())
        {
            await IntegrationDatabaseCleanup.DeleteAllAsync(setup);
            var account =
                AccountTestFactory.CreateNewAccount(AccountTestFactory.FakeIpHasher(), TimeProvider.System);
            setup.Accounts.Add(account);
            await setup.SaveEntitiesAsync(CancellationToken.None);
        }

        await using var ctx1 = fixture.CreateContext();
        await using var ctx2 = fixture.CreateContext();

        var id = await ctx1.Accounts.Select(a => a.Id).FirstAsync();
        var a1 = await ctx1.Accounts.FirstAsync(a => a.Id == id);
        var a2 = await ctx2.Accounts.FirstAsync(a => a.Id == id);

        a1.Update(
            a1.Name,
            null,
            a1.Locale,
            a1.Region,
            a1.Country,
            a1.Timezone,
            TimeProvider.System);
        ctx1.Entry(a1).State = EntityState.Modified;
        await ctx1.SaveEntitiesAsync(CancellationToken.None);

        a2.Update(
            a2.Name,
            null,
            a2.Locale,
            a2.Region,
            a2.Country,
            a2.Timezone,
            TimeProvider.System);
        ctx2.Entry(a2).State = EntityState.Modified;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveEntitiesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Database_OnFreshContainer_MigrationsApplyAndSchemaIsQueryable()
    {
        await using var ctx = fixture.CreateContext();

        var pending = await ctx.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);

        var applied = await ctx.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, m => m.Contains("Init", StringComparison.OrdinalIgnoreCase));

        Assert.True(await ctx.Database.CanConnectAsync());
        await ctx.Accounts.CountAsync();
    }
}
