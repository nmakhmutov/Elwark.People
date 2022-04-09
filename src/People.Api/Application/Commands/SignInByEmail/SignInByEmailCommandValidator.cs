using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
{
    public SignInByEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignInByEmailCommand.Email));

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required);
    }
}
