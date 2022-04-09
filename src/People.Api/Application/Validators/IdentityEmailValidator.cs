using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

internal sealed class IdentityEmailValidator : AbstractValidator<Identity.Email>
{
    public IdentityEmailValidator()
    {
        RuleFor(x => x.Type)
            .Equal(IdentityType.Email);

        RuleFor(x => x.Value)
            .NotEmpty()
            .EmailAddress()
            .WithErrorCode(ExceptionCodes.EmailIncorrectFormat);
    }
}
