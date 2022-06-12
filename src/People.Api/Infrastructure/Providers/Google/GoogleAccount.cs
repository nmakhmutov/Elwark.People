using System.Globalization;
using System.Net.Mail;

namespace People.Api.Infrastructure.Providers.Google;

internal sealed class GoogleAccount
{
    public GoogleAccount(string identity, MailAddress email, bool isEmailVerified, string? firstName,
        string? lastName, Uri? picture, CultureInfo? locale)
    {
        Identity = identity;
        Email = email;
        IsEmailVerified = isEmailVerified;
        FirstName = firstName;
        LastName = lastName;
        Picture = picture;
        Locale = locale;
    }

    public string Identity { get; }
    
    public MailAddress Email { get; }

    public bool IsEmailVerified { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public Uri? Picture { get; }

    public CultureInfo? Locale { get; }
}
