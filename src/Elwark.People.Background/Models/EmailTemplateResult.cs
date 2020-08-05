namespace Elwark.People.Background.Models
{
    public class EmailTemplateResult
    {
        public EmailTemplateResult(string subject, string body)
        {
            Subject = subject;
            Body = body;
        }

        public string Subject { get; }

        public string Body { get; }
    }
}