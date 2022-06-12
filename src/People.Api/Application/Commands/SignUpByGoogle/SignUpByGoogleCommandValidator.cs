using FluentValidation;

namespace People.Api.Application.Commands.SignUpByGoogle;

internal sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
{
    public SignUpByGoogleCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
