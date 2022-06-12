using System.Net.Mail;

namespace People.Domain.Exceptions;

public sealed class EmailException : PeopleException
{
    public EmailException(MailAddress email, string code, string? message = null)
        : base(nameof(EmailException),code, message) =>
        Email = email;

    public MailAddress Email { get; }

    public static EmailException NotFound(MailAddress email) =>
        new(email, nameof(NotFound), $"Email '{email}' not found");

    public static EmailException AlreadyCreated(MailAddress email) =>
        new(email, nameof(AlreadyCreated), $"Email '{email}' already created");

    public static EmailException NotConfirmed(MailAddress email) =>
        new(email, nameof(NotConfirmed), $"Email '{email}' not confirmed");
    
    public static EmailException AlreadyConfirmed(MailAddress email) =>
        new(email, nameof(AlreadyConfirmed), $"Email '{email}' already confirmed");
}
