using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByEmail
{
    public sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
    {
        public SignInByEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignInByEmailCommand.Email));

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
