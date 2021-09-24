using FluentValidation;
using People.Account.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.SignInByMicrosoft
{
    public sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
    {
        public SignInByMicrosoftCommandValidator()
        {
            RuleFor(x => x.Identity)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityMicrosoftValidator())
                .OverridePropertyName(nameof(SignInByMicrosoftCommand.Identity));
        }
    }
}
