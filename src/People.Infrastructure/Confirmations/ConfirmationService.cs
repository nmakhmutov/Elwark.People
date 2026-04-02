using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using People.Application.Providers.Confirmation;
using People.Domain;
using People.Domain.Entities;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationService : IConfirmationService
{
    private const int ConfirmationLength = 6;

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

    private readonly HybridCache _cache;
    private readonly PeopleDbContext _dbContext;
    private readonly AppSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    public ConfirmationService(
        PeopleDbContext dbContext,
        HybridCache cache,
        IOptions<AppSecurityOptions> options,
        TimeProvider timeProvider
    )
    {
        _cache = cache;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<ConfirmationResult> SignInAsync(AccountId id, CancellationToken ct)
    {
        var confirmation = await EncodeAsync(id, "SignIn", ct);

        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public Task<AccountId> SignInAsync(string token, string code, CancellationToken ct) =>
        DecodeAsync(ConventToGuid(token), "SignIn", code, ct);

    public async Task<ConfirmationResult> SignUpAsync(AccountId id, CancellationToken ct)
    {
        var confirmation = await EncodeAsync(id, "SignUp", ct);

        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public Task<AccountId> SignUpAsync(string token, string code, CancellationToken ct) =>
        DecodeAsync(ConventToGuid(token), "SignUp", code, ct);

    public async Task<ConfirmationResult> VerifyEmailAsync(AccountId id, MailAddress email, CancellationToken ct)
    {
        var confirmation = await EncodeAsync(id, "EmailVerify", ct);

        var bytes = Encrypt(confirmation.Id.ToByteArray().Concat(Encoding.UTF8.GetBytes(email.Address)).ToArray());

        return new ConfirmationResult(Convert.ToBase64String(bytes), confirmation.Code);
    }

    public async Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct)
    {
        try
        {
            var bytes = Decrypt(Convert.FromBase64String(token));
            var id = new Guid(bytes[..16]);
            var email = new MailAddress(Encoding.UTF8.GetString(bytes[16..]));

            var accountId = await DecodeAsync(id, "EmailVerify", code, ct);

            return new EmailConfirmation(accountId, email);
        }
        catch (ConfirmationException)
        {
            throw;
        }
        catch
        {
            throw ConfirmationException.Mismatch();
        }
    }

    public Task<int> DeleteAsync(AccountId id, CancellationToken ct) =>
        _dbContext.Confirmations.Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);

    private byte[] Encrypt(byte[] bytes)
    {
        using var aes = Aes.Create();
        aes.Key = _options.AppKey;
        aes.IV = _options.AppVector;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private byte[] Decrypt(byte[] bytes)
    {
        using var aes = Aes.Create();
        aes.Key = _options.AppKey;
        aes.IV = _options.AppVector;

        using var encryptor = aes.CreateDecryptor();
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private async Task<Confirmation> EncodeAsync(AccountId id, string type, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow();

        var ttl = await _cache.GetOrCreateAsync(
            $"ppl-conf-lk-{type}-{id}",
            _ => ValueTask.FromResult(now),
            new HybridCacheEntryOptions
            {
                Expiration = LockTtl,
                LocalCacheExpiration = LockTtl,
            },
            null,
            ct
        );

        if (now != ttl)
            throw ConfirmationException.AlreadySent();

        var confirmation = await _dbContext.Confirmations
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct);

        if (confirmation is not null)
            return confirmation;

        var code = CreateCode(ConfirmationLength);

        var entity = new Confirmation(id, code, type, now, CodeTtl);
        await _dbContext.Confirmations.AddAsync(entity, ct);

        await _dbContext.SaveChangesAsync(ct);

        return entity;
    }

    private async Task<AccountId> DecodeAsync(Guid id, string type, string code, CancellationToken ct)
    {
        var confirmation = await _dbContext.Confirmations
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ConfirmationException.NotFound();

        if (!string.Equals(confirmation.Type, type, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        if (!string.Equals(confirmation.Code, code, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        return confirmation.AccountId;
    }

    private static Guid ConventToGuid(string token)
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

    private static string CreateCode(int length)
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        return RandomNumberGenerator.GetString(chars, length);
    }
}
