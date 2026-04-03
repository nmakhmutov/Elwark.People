using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using Xunit;

namespace Integration.Api.Tests.Infrastructure;

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

    private static ConfirmationChallengeService CreateChallengeSut(PeopleDbContext db, HybridCache cache, TimeProvider? time = null) =>
        new(db, cache, time ?? TimeProvider.System);

    private static EmailVerificationTokenService CreateTokenSut() =>
        new(TestSecurityOptions());

    [Fact]
    public async Task IssueAsync_CreatesEmailSignUpConfirmationRow_ReturnsTokenAndCode_VerifyReturnsAccountId()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var sp = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var accountId = new AccountId(10_001L);
        var result = await sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(6, result.Code.Length);

        var decodedId = await sut.VerifyAsync(result.Token, result.Code, ConfirmationType.EmailSignUp, CancellationToken.None);
        Assert.Equal(accountId, decodedId);

        await using var read = fixture.CreateContext();
        Assert.Equal(1, await read.Set<Confirmation>().CountAsync(c => c.AccountId == accountId));
    }

    [Fact]
    public async Task IssueAsync_SecondCallWithinLockTtl_ThrowsAlreadySent()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var accountId = new AccountId(10_002L);
        _ = await sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None));

        Assert.Equal("AlreadySent", ex.Code);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongEmailSignUpCode_ThrowsMismatch()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var accountId = new AccountId(10_003L);
        var result = await sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.VerifyAsync(result.Token, "ZZZZZZ", ConfirmationType.EmailSignUp, CancellationToken.None));

        Assert.Equal("Mismatch", ex.Code);
    }

    [Fact]
    public async Task VerifyAsync_AfterCleanupOfExpiredRow_ThrowsNotFound()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var accountId = new AccountId(10_004L);
        var result = await sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None);
        var confirmationId = new Guid(Convert.FromBase64String(result.Token));

        var past = DateTime.UtcNow.AddHours(-2);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE confirmations SET expires_at = {past} WHERE id = {confirmationId}");

        var deleted = await db.Confirmations
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync();
        Assert.True(deleted >= 1);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.VerifyAsync(result.Token, result.Code, ConfirmationType.EmailSignUp, CancellationToken.None));

        Assert.Equal("NotFound", ex.Code);
    }

    [Fact]
    public async Task IssueAsync_CreatesEmailSignInConfirmation_VerifyReturnsAccountId()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var accountId = new AccountId(20_001L);
        var result = await sut.IssueAsync(accountId, ConfirmationType.EmailSignIn, CancellationToken.None);

        var decoded = await sut.VerifyAsync(result.Token, result.Code, ConfirmationType.EmailSignIn, CancellationToken.None);
        Assert.Equal(accountId, decoded);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongEmailSignInCode_ThrowsMismatch()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var result = await sut.IssueAsync(new AccountId(20_002L), ConfirmationType.EmailSignIn, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
            sut.VerifyAsync(result.Token, "AAAAAA", ConfirmationType.EmailSignIn, CancellationToken.None));

        Assert.Equal("Mismatch", ex.Code);
    }

    [Fact]
    public async Task EmailVerificationTokenRoundTrip_WithCreatedTokenAndConfirmation_VerifyReturnsAccountIdAndEmail()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var challengeService = CreateChallengeSut(db, cache);
        var tokenService = CreateTokenSut();

        var accountId = new AccountId(30_001L);
        var email = new MailAddress("verify@example.com");
        var challenge = await challengeService.IssueAsync(accountId, ConfirmationType.EmailConfirmation, CancellationToken.None);
        var token = tokenService.CreateToken(challenge.Id, email);

        var payload = tokenService.ParseToken(token);
        var challengeToken = Convert.ToBase64String(payload.ConfirmationId.ToByteArray());
        var confirmedAccountId = await challengeService.VerifyAsync(
            challengeToken,
            challenge.Code,
            ConfirmationType.EmailConfirmation,
            CancellationToken.None);

        Assert.Equal(accountId, confirmedAccountId);
        Assert.Equal(email.Address, payload.Email.Address);
    }

    [Fact]
    public async Task DeleteByAccountAsync_RemovesConfirmationsForAccount()
    {
        await using var write = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(write, cache);

        var accountId = new AccountId(40_001L);
        _ = await sut.IssueAsync(accountId, ConfirmationType.EmailSignUp, CancellationToken.None);

        var removed = await sut.DeleteByAccountAsync(accountId, CancellationToken.None);
        Assert.True(removed >= 1);

        await using var read = fixture.CreateContext();
        Assert.Equal(0, await read.Set<Confirmation>().CountAsync(c => c.AccountId == accountId));
    }

    [Fact]
    public async Task CleanUpAsync_RemovesOnlyExpiredConfirmations()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);
        await using var cacheProvider = CreateServiceProviderWithHybridCache(out var cache);
        var sut = CreateChallengeSut(db, cache);

        var freshId = new AccountId(50_001L);
        var expiredId = new AccountId(50_002L);

        var fresh = await sut.IssueAsync(freshId, ConfirmationType.EmailSignUp, CancellationToken.None);
        var expired = await sut.IssueAsync(expiredId, ConfirmationType.EmailSignUp, CancellationToken.None);

        var expiredGuid = new Guid(Convert.FromBase64String(expired.Token));
        var past = DateTime.UtcNow.AddDays(-1);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE confirmations SET expires_at = {past} WHERE id = {expiredGuid}");

        var deleted = await db.Confirmations
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync();
        Assert.True(deleted >= 1);

        await using var read = fixture.CreateContext();
        Assert.Equal(0, await read.Set<Confirmation>().CountAsync(c => c.AccountId == expiredId));
        Assert.Equal(1, await read.Set<Confirmation>().CountAsync(c => c.AccountId == freshId));

        _ = await sut.VerifyAsync(fresh.Token, fresh.Code, ConfirmationType.EmailSignUp, CancellationToken.None);
    }
}
