using FluentValidation;

namespace People.Application.Commands.SignInByEmail;

public sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
{
    public SignInByEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
