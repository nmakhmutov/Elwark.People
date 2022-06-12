using System.Net.Mail;

namespace People.Api.Infrastructure.Providers.Microsoft;

internal sealed class MicrosoftAccount
{
    public MicrosoftAccount(string identity, MailAddress email, string? firstName, string? lastName)
    {
        Identity = identity;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public string Identity { get; }

    public MailAddress Email { get; }

    public string? FirstName { get; }

    public string? LastName { get; }
}
