using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByGoogle;

internal sealed class SignInByGoogleCommandValidator : AbstractValidator<SignInByGoogleCommand>
{
    public SignInByGoogleCommandValidator() =>
        RuleFor(x => x.Identity)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityGoogleValidator())
            .OverridePropertyName(nameof(SignInByGoogleCommand.Identity));
}
