using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using People.Application.Providers.Confirmation;

namespace People.Infrastructure.Confirmations;

internal sealed class EmailVerificationTokenService : IEmailVerificationTokenService
{
    private readonly AppSecurityOptions _options;

    public EmailVerificationTokenService(IOptions<AppSecurityOptions> options) =>
        _options = options.Value;

    public string CreateToken(Guid confirmationId, MailAddress email)
    {
        var payload = ComposePayload(confirmationId, email.Address);
        var encrypted = Transform(payload, encrypt: true);

        return Convert.ToBase64String(encrypted);
    }

    public EmailVerificationTokenPayload ParseToken(string token)
    {
        try
        {
            var decrypted = Transform(Convert.FromBase64String(token), encrypt: false);
            var confirmationId = new Guid(decrypted[..16]);
            var email = new MailAddress(Encoding.UTF8.GetString(decrypted[16..]));

            return new EmailVerificationTokenPayload(confirmationId, email);
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

    private static byte[] ComposePayload(Guid confirmationId, string email)
    {
        var idBytes = confirmationId.ToByteArray();
        var emailBytes = Encoding.UTF8.GetBytes(email);
        var payload = new byte[idBytes.Length + emailBytes.Length];

        idBytes.CopyTo(payload, 0);
        emailBytes.CopyTo(payload, idBytes.Length);

        return payload;
    }

    private byte[] Transform(byte[] bytes, bool encrypt)
    {
        using var aes = Aes.Create();
        aes.Key = _options.AppKey;
        aes.IV = _options.AppVector;

        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        return transform.TransformFinalBlock(bytes, 0, bytes.Length);
    }
}
