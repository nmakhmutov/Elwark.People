using FluentValidation;

namespace People.Application.Commands.SignUpByMicrosoft;

public sealed class SignUpByMicrosoftCommandValidator : AbstractValidator<SignUpByMicrosoftCommand>
{
    public SignUpByMicrosoftCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
