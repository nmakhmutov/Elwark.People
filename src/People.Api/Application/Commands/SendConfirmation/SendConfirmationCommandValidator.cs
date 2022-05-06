using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SendConfirmation;

internal sealed class SendConfirmationCommandValidator : AbstractValidator<SendConfirmationCommand>
{
    public SendConfirmationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().WithErrorCode(ExceptionCodes.Required);

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SendConfirmationCommand.Email));
    }
}
