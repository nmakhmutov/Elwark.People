using System.Threading.Tasks;

namespace People.Api.Infrastructure.EmailBuilder;

public interface IEmailBuilder
{
    Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model);
}
