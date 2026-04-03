using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using People.Application.Providers.Confirmation;
using People.Domain;
using People.Domain.Entities;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationChallengeService : IConfirmationChallengeService
{
    private const int ConfirmationLength = 6;
    private const string ConfirmationChars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

    private static readonly HybridCacheEntryOptions LockCacheOptions = new()
    {
        Expiration = LockTtl,
        LocalCacheExpiration = LockTtl,
    };

    private readonly HybridCache _cache;
    private readonly PeopleDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ConfirmationChallengeService(PeopleDbContext dbContext, HybridCache cache, TimeProvider timeProvider)
    {
        _cache = cache;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ConfirmationChallenge> IssueAsync(AccountId id, ConfirmationType type, CancellationToken ct)
    {
        await AcquireLockAsync(GetLockKey(type, id), ct);

        var db = await _dbContext.Confirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct);

        if (db is not null)
            return MapConfirmationChallenge(db);

        var code = RandomNumberGenerator.GetString(ConfirmationChars, ConfirmationLength);
        var confirmation = new Confirmation(id, code, type, CodeTtl, _timeProvider.UtcNow());
        await _dbContext.Confirmations.AddAsync(confirmation, ct);
        await _dbContext.SaveChangesAsync(ct);

        return MapConfirmationChallenge(confirmation);
    }

    public async Task<AccountId> VerifyAsync(string token, string code, ConfirmationType type, CancellationToken ct)
    {
        var id = ParseChallengeId(token);

        var confirmation = await _dbContext.Confirmations
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VerificationModel(x.AccountId, x.Code, x.Type))
            .FirstOrDefaultAsync(ct) ?? throw ConfirmationException.NotFound();

        if (confirmation.Type != type || !string.Equals(confirmation.Code, code, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        return confirmation.AccountId;
    }

    public Task<int> DeleteByAccountAsync(AccountId id, CancellationToken ct) =>
        _dbContext.Confirmations.Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);

    private static ConfirmationChallenge MapConfirmationChallenge(Confirmation confirmation) =>
        new(confirmation.Id, Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);

    private async Task AcquireLockAsync(string key, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow();
        var lockTimestamp = await _cache.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult(now),
            LockCacheOptions,
            cancellationToken: ct
        );

        if (lockTimestamp != now)
            throw ConfirmationException.AlreadySent();
    }

    private static string GetLockKey(ConfirmationType type, AccountId id) =>
        $"ppl-conf-lk-{type}-{id}";

    private static Guid ParseChallengeId(string token)
    {
        try
        {
            return new Guid(Convert.FromBase64String(token));
        }
        catch
        {
            throw ConfirmationException.Mismatch();
        }
    }

    private sealed record VerificationModel(AccountId AccountId, string Code, ConfirmationType Type);
}
