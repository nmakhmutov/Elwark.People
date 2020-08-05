using Elwark.People.Api.Application.Commands.SignUp;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
    {
        public SignUpByEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}