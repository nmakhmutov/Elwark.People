using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using People.Domain;
using People.Domain.Entities;
using StackExchange.Redis;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationService : IConfirmationService
{
    private const int ConfirmationLength = 6;

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

    private readonly PeopleDbContext _dbContext;
    private readonly AppSecurityOptions _options;
    private readonly IDatabaseAsync _redis;
    private readonly TimeProvider _timeProvider;

    public ConfirmationService(PeopleDbContext dbContext, IConnectionMultiplexer multiplexer,
        IOptions<AppSecurityOptions> options, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _redis = multiplexer.GetDatabase();
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
            var id = new Ulid(bytes[..16]);
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
        _dbContext.Set<Confirmation>().Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);

    public Task<int> CleanUpAsync(CancellationToken ct) =>
        _dbContext.Set<Confirmation>().Where(x => x.ExpiresAt < DateTime.UtcNow).ExecuteDeleteAsync(ct);

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
        var key = $"ppl-conf-lk-{id}";

        if (await _redis.KeyExistsAsync(key))
            throw ConfirmationException.AlreadySent();

        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct);

        if (confirmation is not null)
            return confirmation;

        var now = _timeProvider.UtcNow();
        var code = CreateCode(ConfirmationLength);

        var entity = new Confirmation(id, code, type, now, CodeTtl);
        await _dbContext.AddAsync(entity, ct);

        await _dbContext.SaveChangesAsync(ct);

        await _redis.StringSetAsync(key, true, LockTtl);

        return entity;
    }

    private async Task<AccountId> DecodeAsync(Ulid id, string type, string code, CancellationToken ct)
    {
        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ConfirmationException.NotFound();

        if (!string.Equals(confirmation.Type, type, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        if (!string.Equals(confirmation.Code, code, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        return confirmation.AccountId;
    }

    private static Ulid ConventToGuid(string token)
    {
        try
        {
            return new Ulid(Convert.FromBase64String(token));
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
