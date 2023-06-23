using FluentValidation;

namespace People.Api.Application.Commands.AppendGoogle;

internal sealed class AppendGoogleCommandValidator : AbstractValidator<AppendGoogleCommand>
{
    public AppendGoogleCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
