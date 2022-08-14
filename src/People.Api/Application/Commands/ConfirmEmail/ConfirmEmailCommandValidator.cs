using FluentValidation;

namespace People.Api.Application.Commands.ConfirmEmail;

internal sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
