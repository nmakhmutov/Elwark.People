using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

internal sealed class IdentityEmailValidator : AbstractValidator<EmailIdentity>
{
    public IdentityEmailValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .EmailAddress()
            .WithErrorCode(ExceptionCodes.EmailIncorrectFormat);
    }
}
