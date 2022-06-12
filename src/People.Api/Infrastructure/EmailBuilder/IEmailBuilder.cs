namespace People.Api.Infrastructure.EmailBuilder;

internal interface IEmailBuilder
{
    Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model);
}
