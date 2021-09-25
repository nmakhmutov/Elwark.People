using FluentValidation;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.SignInByEmail
{
    public sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
    {
        public SignInByEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignInByEmailCommand.Email));

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
