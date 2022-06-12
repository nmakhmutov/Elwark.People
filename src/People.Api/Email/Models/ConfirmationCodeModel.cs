using People.Api.Infrastructure.EmailBuilder;

namespace People.Api.Email.Models;

internal sealed record ConfirmationCodeModel(int Code) : ITemplateModel;
