using FluentValidation;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
{
    public SignInByEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
