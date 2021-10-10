using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
{
    public SignInByMicrosoftCommandValidator() =>
        RuleFor(x => x.Identity)
            .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
            .SetValidator(new IdentityMicrosoftValidator())
            .OverridePropertyName(nameof(SignInByMicrosoftCommand.Identity));
}
