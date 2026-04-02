namespace People.Infrastructure.EmailBuilder;

public interface IEmailBuilder
{
    Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model);
}
