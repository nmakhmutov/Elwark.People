using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using People.Domain.SeedWork;
using StackExchange.Redis;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationService : IConfirmationService
{
    private const int ConfirmationLength = 5;

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

    private readonly PeopleDbContext _dbContext;
    private readonly AppSecurityOptions _options;
    private readonly IDatabaseAsync _redis;

    public ConfirmationService(PeopleDbContext dbContext, IConnectionMultiplexer multiplexer,
        IOptions<AppSecurityOptions> options)
    {
        _dbContext = dbContext;
        _redis = multiplexer.GetDatabase();
        _options = options.Value;
    }

    public async Task<AccountConfirmation> SignInAsync(string token, string code, CancellationToken ct)
    {
        if (TryGetGuid(token, out var id))
            return await CheckAsync(id, "SignIn", code, ct);

        throw ConfirmationException.Mismatch();
    }

    public async Task<ConfirmationResult> SignInAsync(long id, ITimeProvider time, CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "SignIn", time, ct);
        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public async Task<AccountConfirmation> SignUpAsync(string token, string code, CancellationToken ct)
    {
        if (TryGetGuid(token, out var id))
            return await CheckAsync(id, "SignUp", code, ct);

        throw ConfirmationException.Mismatch();
    }

    public async Task<ConfirmationResult> SignUpAsync(long id, ITimeProvider time, CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "SignUp", time, ct);
        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public async Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct)
    {
        Guid id;
        MailAddress email;

        try
        {
            var bytes = Decrypt(Convert.FromBase64String(token));
            id = new Guid(bytes[..16]);
            email = new MailAddress(Encoding.UTF8.GetString(bytes[16..]));
        }
        catch
        {
            throw ConfirmationException.Mismatch();
        }

        var check = await CheckAsync(id, "EmailVerify", code, ct);
        return new EmailConfirmation(check.AccountId, email);
    }

    public Task<int> DeleteAsync(DateTime now, CancellationToken ct) =>
        _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM confirmations WHERE expires_at < '{now:O}'", ct);

    public Task<int> DeleteAsync(long id, CancellationToken ct) =>
        _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM confirmations WHERE account_id = {id}", ct);

    public async Task<ConfirmationResult> VerifyEmailAsync(long id, MailAddress email, ITimeProvider time,
        CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "EmailVerify", time, ct);
        var bytes = Encrypt(confirmation.Id.ToByteArray().Concat(Encoding.UTF8.GetBytes(email.Address)).ToArray());

        return new ConfirmationResult(Convert.ToBase64String(bytes), confirmation.Code);
    }

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

    private async Task<AccountConfirmation> CheckAsync(Guid id, string type, string code, CancellationToken ct)
    {
        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ConfirmationException.NotFound();

        if (!string.Equals(confirmation.Type, type, StringComparison.InvariantCultureIgnoreCase))
            throw ConfirmationException.Mismatch();

        if (!string.Equals(confirmation.Code, code, StringComparison.InvariantCultureIgnoreCase))
            throw ConfirmationException.Mismatch();

        return new AccountConfirmation(confirmation.AccountId);
    }

    private async Task<Confirmation> GetOrCreateAsync(long id, string type, ITimeProvider time, CancellationToken ct)
    {
        var key = $"ppl-conf-lk-{id}";

        if (await _redis.KeyExistsAsync(key))
            throw ConfirmationException.AlreadySent();

        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct);

        if (confirmation is not null)
            return confirmation;

        var guid = CreateSortedGuid(id, time.Now);
        var code = Generate(ConfirmationLength);

        var entity = new Confirmation(guid, id, code, type, time.Now, CodeTtl);
        await _dbContext.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _redis.StringSetAsync(key, true, LockTtl);

        return entity;
    }

    private static Guid CreateSortedGuid(long id, DateTime time)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes[..8]);
        BitConverter.GetBytes(time.Ticks).CopyTo(bytes[8..]);

        return new Guid(bytes);
    }

    private static bool TryGetGuid(string token, out Guid guid)
    {
        try
        {
            guid = new Guid(Convert.FromBase64String(token));
            return true;
        }
        catch
        {
            guid = Guid.Empty;
            return false;
        }
    }

    private static string Generate(int length)
    {
        const string chars = "123456789ABCDEFGHIJKLMNPQRSTUVWXYZ";

        using var generator = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        generator.GetBytes(bytes);

        return new string(bytes.Select(x => chars[x % chars.Length]).ToArray());
    }
}
