using FluentValidation;

namespace People.Application.Commands.SignInByMicrosoft;

public sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
{
    public SignInByMicrosoftCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
