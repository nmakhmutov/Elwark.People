using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

public sealed class IdentityEmailValidator : AbstractValidator<Identity.Email>
{
    public IdentityEmailValidator()
    {
        RuleFor(x => x.Type)
            .Equal(IdentityType.Email);

        RuleFor(x => x.Value)
            .NotEmpty()
            .EmailAddress()
            .WithErrorCode(ElwarkExceptionCodes.EmailIncorrectFormat);
    }
}
