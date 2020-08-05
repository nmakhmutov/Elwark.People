using Elwark.People.Api.Application.Commands.SignUp;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignUpByMicrosoftCommandValidator : AbstractValidator<SignUpByMicrosoftCommand>
    {
        public SignUpByMicrosoftCommandValidator()
        {
            RuleFor(x => x.Microsoft)
                .NotNull();

            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}