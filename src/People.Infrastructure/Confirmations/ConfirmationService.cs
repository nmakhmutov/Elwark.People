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
    private const int ConfirmationLength = 6;

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

    public Task<AccountConfirmation> SignInAsync(string token, string code, CancellationToken ct) =>
        CheckAsync(ConventToGuid(token), "SignIn", code, ct);

    public async Task<ConfirmationResult> SignInAsync(long id, ITimeProvider time, CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "SignIn", time, ct)
            .ConfigureAwait(false);

        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public Task<AccountConfirmation> SignUpAsync(string token, string code, CancellationToken ct) =>
        CheckAsync(ConventToGuid(token), "SignUp", code, ct);

    public async Task<ConfirmationResult> SignUpAsync(long id, ITimeProvider time, CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "SignUp", time, ct)
            .ConfigureAwait(false);

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

        var check = await CheckAsync(id, "EmailVerify", code, ct)
            .ConfigureAwait(false);

        return new EmailConfirmation(check.AccountId, email);
    }

    public Task<int> DeleteAsync(DateTime now, CancellationToken ct) =>
        _dbContext.Set<Confirmation>().Where(x => x.ExpiresAt < now).ExecuteDeleteAsync(ct);

    public Task<int> DeleteAsync(long id, CancellationToken ct) =>
        _dbContext.Set<Confirmation>().Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);

    public async Task<ConfirmationResult> VerifyEmailAsync(long id, MailAddress email, ITimeProvider time,
        CancellationToken ct)
    {
        var confirmation = await GetOrCreateAsync(id, "EmailVerify", time, ct)
            .ConfigureAwait(false);

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
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw ConfirmationException.NotFound();

        if (!string.Equals(confirmation.Type, type, StringComparison.InvariantCultureIgnoreCase))
            throw ConfirmationException.Mismatch();

        if (!string.Equals(confirmation.Code, code, StringComparison.InvariantCultureIgnoreCase))
            throw ConfirmationException.Mismatch();

        return new AccountConfirmation(confirmation.AccountId);
    }

    private async Task<Confirmation> GetOrCreateAsync(long id, string type, ITimeProvider time, CancellationToken ct)
    {
        var key = $"ppl-conf-lk-{id}";

        if (await _redis.KeyExistsAsync(key).ConfigureAwait(false))
            throw ConfirmationException.AlreadySent();

        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct)
            .ConfigureAwait(false);

        if (confirmation is not null)
            return confirmation;

        var guid = CreateSortedGuid(id, time.Now);
        var code = Generate(ConfirmationLength);

        var entity = new Confirmation(guid, id, code, type, time.Now, CodeTtl);
        await _dbContext.AddAsync(entity, ct)
            .ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(ct)
            .ConfigureAwait(false);

        await _redis.StringSetAsync(key, true, LockTtl)
            .ConfigureAwait(false);

        return entity;
    }

    private static Guid CreateSortedGuid(long id, DateTime time)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.GetBytes(time.Ticks).CopyTo(bytes[..8]);
        BitConverter.GetBytes(id).CopyTo(bytes[8..]);

        return new Guid(bytes);
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

    private static string Generate(int length)
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        
        return RandomNumberGenerator.GetString(chars, length);
    }
}
