using FluentValidation;

namespace People.Api.Application.Commands.SignInByGoogle;

internal sealed class SignInByGoogleCommandValidator : AbstractValidator<SignInByGoogleCommand>
{
    public SignInByGoogleCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
