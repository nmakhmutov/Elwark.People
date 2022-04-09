using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
{
    public SignInByMicrosoftCommandValidator() =>
        RuleFor(x => x.Microsoft)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityMicrosoftValidator())
            .OverridePropertyName(nameof(SignInByMicrosoftCommand.Microsoft));
}