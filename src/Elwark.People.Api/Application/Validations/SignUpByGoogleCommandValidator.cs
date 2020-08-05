using Elwark.People.Api.Application.Commands.SignUp;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
    {
        public SignUpByGoogleCommandValidator()
        {
            RuleFor(x => x.Google)
                .NotNull();

            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}