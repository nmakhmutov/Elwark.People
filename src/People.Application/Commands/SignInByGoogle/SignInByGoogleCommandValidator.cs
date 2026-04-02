using FluentValidation;

namespace People.Application.Commands.SignInByGoogle;

public sealed class SignInByGoogleCommandValidator : AbstractValidator<SignInByGoogleCommand>
{
    public SignInByGoogleCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
