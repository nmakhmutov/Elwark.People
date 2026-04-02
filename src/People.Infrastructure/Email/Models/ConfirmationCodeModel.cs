using People.Infrastructure.EmailBuilder;

namespace People.Infrastructure.Email.Models;

internal sealed record ConfirmationCodeModel(string Code) : ITemplateModel;
