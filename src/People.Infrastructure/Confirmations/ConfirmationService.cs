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
    private const string LockTemplate = "ppl-conf-lk-{0}";

    private static readonly Random Random = new();
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

    public async Task<Result<AccountConfirmation>> CheckSignInAsync(string token, int code)
    {
        if (TryGetGuid(token, out var id))
            return await CheckAsync(id, "SignIn", code);

        return new Result<AccountConfirmation>(ConfirmationException.Mismatch());
    }

    public async Task<ConfirmationResult> CreateSignInAsync(long id, ITimeProvider time)
    {
        var confirmation = await GetOrCreateAsync(id, "SignIn", time);
        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public async Task<Result<AccountConfirmation>> CheckSignUpAsync(string token, int code)
    {
        if (TryGetGuid(token, out var id))
            return await CheckAsync(id, "SignUp", code);

        return new Result<AccountConfirmation>(ConfirmationException.Mismatch());
    }

    public async Task<ConfirmationResult> CreateSignUpAsync(long id, ITimeProvider time)
    {
        var confirmation = await GetOrCreateAsync(id, "SignUp", time);
        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public async Task<Result<EmailConfirmation>> CheckEmailVerifyAsync(string token, int code)
    {
        try
        {
            var bytes = Decrypt(Convert.FromBase64String(token));
            var id = new Guid(bytes[..16]);
            var email = new MailAddress(Encoding.UTF8.GetString(bytes[16..]));

            var check = await CheckAsync(id, "EmailVerify", code);
            if (check.HasException)
                return new Result<EmailConfirmation>(check.Exception);

            return new EmailConfirmation(check.Value.AccountId, email);
        }
        catch
        {
            return new Result<EmailConfirmation>(ConfirmationException.Mismatch());
        }
    }

    public async Task<ConfirmationResult> CreateEmailVerifyAsync(long id, MailAddress email, ITimeProvider time)
    {
        var confirmation = await GetOrCreateAsync(id, "EmailVerify", time);
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

    private async Task<Result<AccountConfirmation>> CheckAsync(Guid id, string type, int code)
    {
        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (confirmation is null)
            return new Result<AccountConfirmation>(ConfirmationException.NotFound());

        if (confirmation.Type != type)
            return new Result<AccountConfirmation>(ConfirmationException.Mismatch());

        if (confirmation.Code != code)
            return new Result<AccountConfirmation>(ConfirmationException.Mismatch());

        return new Result<AccountConfirmation>(new AccountConfirmation(confirmation.AccountId));
    }

    private async Task<Confirmation> GetOrCreateAsync(long id, string type, ITimeProvider time)
    {
        var key = string.Format(LockTemplate, id);
        
        if (await _redis.KeyExistsAsync(key))
            throw ConfirmationException.AlreadySent();
        
        await _redis.StringSetAsync(key, true, LockTtl);
        
        var confirmation = await _dbContext.Set<Confirmation>()
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type);

        if (confirmation is not null)
            return confirmation;

        var entity = new Confirmation(CreateSortedGuid(time.Now, id), id, Generate(), type, time.Now, CodeTtl);
        await _dbContext.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        
        return entity;
    }

    private static Guid CreateSortedGuid(DateTime time, long id)
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

    private static int Generate() =>
        Random.Next(1_000, 10_000);
}
