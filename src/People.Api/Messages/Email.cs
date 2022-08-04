using System.Net.Mail;

// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class Email
{
    public MailAddress ToMailAddress() =>
        new(Value);
}
