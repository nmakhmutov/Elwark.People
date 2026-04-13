using System.Net.Mail;

namespace People.Application.Providers.Microsoft;

public sealed class MicrosoftAccount
{
    public string Identity { get; }

    public MailAddress Email { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public MicrosoftAccount(string identity, MailAddress email, string? firstName, string? lastName)
    {
        Identity = identity;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}
