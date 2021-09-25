using FluentValidation;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.SignInByGoogle
{
    public sealed class SignInByGoogleCommandValidator : AbstractValidator<SignInByGoogleCommand>
    {
        public SignInByGoogleCommandValidator()
        {
            RuleFor(x => x.Identity)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityGoogleValidator())
                .OverridePropertyName(nameof(SignInByGoogleCommand.Identity));
        }
    }
}
