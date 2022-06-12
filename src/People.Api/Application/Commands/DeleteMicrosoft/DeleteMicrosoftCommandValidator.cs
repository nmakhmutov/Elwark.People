using FluentValidation;

namespace People.Api.Application.Commands.DeleteMicrosoft;

internal sealed class DeleteMicrosoftCommandValidator : AbstractValidator<DeleteMicrosoftCommand>
{
    public DeleteMicrosoftCommandValidator()
    {
        RuleFor(x => x.Identity)
            .NotNull();
    }
}
