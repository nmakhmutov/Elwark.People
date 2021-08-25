using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.AcceptEmail
{
    public sealed class AcceptEmailCommandValidator : AbstractValidator<AcceptEmailCommand>
    {
        public AcceptEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AcceptEmailCommand.Email));

            RuleFor(x => x.Subject)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);

            RuleFor(x => x.Body)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
