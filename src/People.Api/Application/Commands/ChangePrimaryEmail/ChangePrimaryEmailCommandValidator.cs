using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ChangePrimaryEmail;

internal sealed class ChangePrimaryEmailCommandValidator : AbstractValidator<ChangePrimaryEmailCommand>
{
    public ChangePrimaryEmailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(ChangePrimaryEmailCommand.Email));
    }
}
