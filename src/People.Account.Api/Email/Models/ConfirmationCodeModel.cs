using People.Account.Api.Infrastructure.EmailBuilder;

namespace People.Account.Api.Email.Models
{
    public sealed record ConfirmationCodeModel(uint Code) : ITemplateModel;
}
