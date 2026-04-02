using FluentValidation;

namespace People.Application.Commands.SignUpByEmail;

public sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
{
    public SignUpByEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
