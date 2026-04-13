using System.Globalization;
using System.Net.Mail;

namespace People.Application.Providers.Google;

public sealed class GoogleAccount
{
    public string Identity { get; }

    public MailAddress Email { get; }

    public bool IsEmailVerified { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public Uri? Picture { get; }

    public CultureInfo? Locale { get; }

    public GoogleAccount(
        string identity,
        MailAddress email,
        bool isEmailVerified,
        string? firstName,
        string? lastName,
        Uri? picture,
        CultureInfo? locale
    )
    {
        Identity = identity;
        Email = email;
        IsEmailVerified = isEmailVerified;
        FirstName = firstName;
        LastName = lastName;
        Picture = picture;
        Locale = locale;
    }
}
