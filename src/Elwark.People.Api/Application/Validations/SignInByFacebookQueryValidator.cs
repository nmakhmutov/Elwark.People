using Elwark.People.Api.Application.Queries.SignIn;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignInByFacebookQueryValidator : AbstractValidator<SignInByFacebookQuery>
    {
        public SignInByFacebookQueryValidator()
        {
            RuleFor(x => x.Facebook)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}