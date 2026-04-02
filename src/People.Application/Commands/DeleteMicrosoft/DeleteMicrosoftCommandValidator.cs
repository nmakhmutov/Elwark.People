using FluentValidation;

namespace People.Application.Commands.DeleteMicrosoft;

public sealed class DeleteMicrosoftCommandValidator : AbstractValidator<DeleteMicrosoftCommand>
{
    public DeleteMicrosoftCommandValidator() =>
        RuleFor(x => x.Identity)
            .NotNull();
}
