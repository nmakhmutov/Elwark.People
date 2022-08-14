using FluentValidation;

namespace People.Api.Application.Commands.SignUpByEmail;

internal sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
{
    public SignUpByEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
