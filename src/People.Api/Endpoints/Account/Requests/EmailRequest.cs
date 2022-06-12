using FluentValidation;

namespace People.Api.Endpoints.Account.Requests;

internal sealed record EmailRequest(string Email)
{
    internal sealed class Validator : AbstractValidator<EmailRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
