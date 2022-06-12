using FluentValidation;

namespace People.Api.Application.Commands.DeleteGoogle;

internal sealed class DeleteGoogleCommandValidator : AbstractValidator<DeleteGoogleCommand>
{
    public DeleteGoogleCommandValidator()
    {
        RuleFor(x => x.Identity)
            .NotNull();
    }
}
