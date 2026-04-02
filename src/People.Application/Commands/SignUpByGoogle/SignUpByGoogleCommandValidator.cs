using FluentValidation;

namespace People.Application.Commands.SignUpByGoogle;

public sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
{
    public SignUpByGoogleCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
