using FluentValidation;

namespace People.Application.Commands.DeleteGoogle;

public sealed class DeleteGoogleCommandValidator : AbstractValidator<DeleteGoogleCommand>
{
    public DeleteGoogleCommandValidator() =>
        RuleFor(x => x.Identity)
            .NotNull();
}
