using FluentValidation;

namespace People.Gateway.Endpoints.Account.Request;

public sealed record UpdatePrimaryEmailRequest(string Email)
{
    public sealed class Validator : AbstractValidator<UpdatePrimaryEmailRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
