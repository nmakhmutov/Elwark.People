using FluentValidation;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

internal sealed class SignUpByMicrosoftCommandValidator : AbstractValidator<SignUpByMicrosoftCommand>
{
    public SignUpByMicrosoftCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
