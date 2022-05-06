using FluentValidation;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.CheckConfirmation;

internal sealed class CheckConfirmationCommandValidator : AbstractValidator<CheckConfirmationCommand>
{
    public CheckConfirmationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .Length(24);

        RuleFor(x => x.Code)
            .NotEmpty();
    }
}
