using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Hybrid;
using People.Domain.Entities;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using Xunit;

namespace People.IntegrationTests.Infrastructure;

[Collection(nameof(PostgresCollection))]
public sealed class ConfirmationServiceTests(PostgreSqlFixture fixture)
{
    /// <summary>AES-256 key (32 UTF-8 bytes) and 16-byte IV string for <see cref="AppSecurityOptions"/>.</summary>
    private static IOptions<AppSecurityOptions> TestSecurityOptions() =>
        Options.Create(new AppSecurityOptions(new string('K', 32), new string('V', 16)));

    private static ServiceProvider CreateServiceProviderWithHybridCache(out HybridCache cache)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        cache = provider.GetRequiredService<HybridCache>();
        return provider;
    }

    private static ConfirmationService CreateSut(PeopleDbContext db, HybridCache cache, TimeProvider? time = null) =>
        new(db, cache, TestSecurityOptions(), time ?? TimeProvider.System);

    [Fact]
    public async Task SignUpAsync_CreatesConfirmationRow_ReturnsTokenAndCode_VerifyReturnsAccountId()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var sp = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(10_001L);
        var result = await sut.SignUpAsync(accountId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(6, result.Code.Length);

        var decodedId = await sut.SignUpAsync(result.Token, result.Code, CancellationToken.None);
        Assert.Equal(accountId, decodedId);

        await using var read = fixture.CreateContext(new NoOpMediator());
        Assert.Equal(1, await read.Set<Confirmation>().CountAsync(c => c.AccountId == accountId));
    }

    [Fact]
    public async Task SignUpAsync_SecondCallWithinLockTtl_ThrowsAlreadySent()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(10_002L);
        _ = await sut.SignUpAsync(accountId, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() => sut.SignUpAsync(accountId, CancellationToken.None));

        Assert.Equal("AlreadySent", ex.Code);
    }

    [Fact]
    public async Task SignUpAsync_VerifyWithWrongCode_ThrowsMismatch()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(10_003L);
        var result = await sut.SignUpAsync(accountId, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.SignUpAsync(result.Token, "ZZZZZZ", CancellationToken.None));

        Assert.Equal("Mismatch", ex.Code);
    }

    [Fact]
    public async Task SignUpAsync_AfterCleanupOfExpiredRow_VerifyThrowsNotFound()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(10_004L);
        var result = await sut.SignUpAsync(accountId, CancellationToken.None);
        var confirmationId = new Guid(Convert.FromBase64String(result.Token));

        var past = DateTime.UtcNow.AddHours(-2);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE confirmations SET expires_at = {past} WHERE id = {confirmationId}");

        var deleted = await sut.CleanUpAsync(CancellationToken.None);
        Assert.True(deleted >= 1);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.SignUpAsync(result.Token, result.Code, CancellationToken.None));

        Assert.Equal("NotFound", ex.Code);
    }

    [Fact]
    public async Task SignInAsync_CreatesConfirmation_VerifyReturnsAccountId()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(20_001L);
        var result = await sut.SignInAsync(accountId, CancellationToken.None);

        var decoded = await sut.SignInAsync(result.Token, result.Code, CancellationToken.None);
        Assert.Equal(accountId, decoded);
    }

    [Fact]
    public async Task SignInAsync_VerifyWithWrongCode_ThrowsMismatch()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var result = await sut.SignInAsync(new AccountId(20_002L), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.SignInAsync(result.Token, "AAAAAA", CancellationToken.None));

        Assert.Equal("Mismatch", ex.Code);
    }

    [Fact]
    public async Task VerifyEmailAsync_CreatesConfirmation_VerifyReturnsAccountIdAndEmail()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var accountId = new AccountId(30_001L);
        var email = new MailAddress("verify@example.com");
        var result = await sut.VerifyEmailAsync(accountId, email, CancellationToken.None);

        var confirmed = await sut.VerifyEmailAsync(result.Token, result.Code, CancellationToken.None);

        Assert.Equal(accountId, confirmed.AccountId);
        Assert.Equal(email.Address, confirmed.Email.Address);
    }

    [Fact]
    public async Task DeleteAsync_RemovesConfirmationsForAccount()
    {
        await using var write = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(write, cache);

        var accountId = new AccountId(40_001L);
        _ = await sut.SignUpAsync(accountId, CancellationToken.None);

        var removed = await sut.DeleteAsync(accountId, CancellationToken.None);
        Assert.True(removed >= 1);

        await using var read = fixture.CreateContext(new NoOpMediator());
        Assert.Equal(0, await read.Set<Confirmation>().CountAsync(c => c.AccountId == accountId));
    }

    [Fact]
    public async Task CleanUpAsync_RemovesOnlyExpiredConfirmations()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateSut(db, cache);

        var freshId = new AccountId(50_001L);
        var expiredId = new AccountId(50_002L);

        var fresh = await sut.SignUpAsync(freshId, CancellationToken.None);
        var expired = await sut.SignUpAsync(expiredId, CancellationToken.None);

        var expiredGuid = new Guid(Convert.FromBase64String(expired.Token));
        var past = DateTime.UtcNow.AddDays(-1);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE confirmations SET expires_at = {past} WHERE id = {expiredGuid}");

        var deleted = await sut.CleanUpAsync(CancellationToken.None);
        Assert.True(deleted >= 1);

        await using var read = fixture.CreateContext(new NoOpMediator());
        Assert.Equal(0, await read.Set<Confirmation>().CountAsync(c => c.AccountId == expiredId));
        Assert.Equal(1, await read.Set<Confirmation>().CountAsync(c => c.AccountId == freshId));

        _ = await sut.SignUpAsync(fresh.Token, fresh.Code, CancellationToken.None);
    }
}
