using System.Threading.Tasks;

namespace People.Account.Api.Infrastructure.EmailBuilder
{
    public interface IEmailBuilder
    {
        Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model);
    }
}