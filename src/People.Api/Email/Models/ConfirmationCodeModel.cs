using People.Api.Infrastructure.EmailBuilder;

namespace People.Api.Email.Models
{
    public sealed record ConfirmationCodeModel(uint Code) : ITemplateModel;
}
