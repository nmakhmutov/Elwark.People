using FluentValidation;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ConfuteConnection;

internal sealed class ConfuteConnectionCommandValidator : AbstractValidator<ConfuteConnectionCommand>
{
    public ConfuteConnectionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required);

        RuleFor(x => x.Identity)
            .NotNull();
    }
}
