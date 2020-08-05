using Elwark.People.Api.Application.Queries.SignIn;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignInByGoogleQueryValidator : AbstractValidator<SignInByGoogleQuery>
    {
        public SignInByGoogleQueryValidator()
        {
            RuleFor(x => x.Google)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}