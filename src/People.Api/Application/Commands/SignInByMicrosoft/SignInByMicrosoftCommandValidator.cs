using FluentValidation;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
{
    public SignInByMicrosoftCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
