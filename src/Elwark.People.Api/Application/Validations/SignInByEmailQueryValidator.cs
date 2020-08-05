using Elwark.People.Api.Application.Queries.SignIn;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignInByEmailQueryValidator : AbstractValidator<SignInByEmailQuery>
    {
        public SignInByEmailQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}