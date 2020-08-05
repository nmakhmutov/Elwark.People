using Elwark.People.Api.Application.Commands.SignUp;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignUpByFacebookCommandValidator : AbstractValidator<SignUpByFacebookCommand>
    {
        public SignUpByFacebookCommandValidator()
        {
            RuleFor(x => x.Facebook)
                .NotNull();

            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}