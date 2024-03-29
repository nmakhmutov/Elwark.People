using FluentValidation;

namespace People.Api.Endpoints.Account.Requests;

internal sealed record VerifyRequest(string Token, string Code)
{
    internal sealed class Validator : AbstractValidator<VerifyRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Token)
                .NotEmpty();

            RuleFor(x => x.Code)
                .NotEmpty();
        }
    }
}
