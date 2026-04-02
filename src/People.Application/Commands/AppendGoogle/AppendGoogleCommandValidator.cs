using FluentValidation;

namespace People.Application.Commands.AppendGoogle;

public sealed class AppendGoogleCommandValidator : AbstractValidator<AppendGoogleCommand>
{
    public AppendGoogleCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
