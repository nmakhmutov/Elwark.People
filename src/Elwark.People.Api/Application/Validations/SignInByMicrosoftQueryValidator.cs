using Elwark.People.Api.Application.Queries.SignIn;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignInByMicrosoftQueryValidator : AbstractValidator<SignInByMicrosoftQuery>
    {
        public SignInByMicrosoftQueryValidator()
        {
            RuleFor(x => x.Microsoft)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}