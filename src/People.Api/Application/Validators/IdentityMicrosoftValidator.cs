using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

public sealed class IdentityMicrosoftValidator : AbstractValidator<Identity.Microsoft>
{
    public IdentityMicrosoftValidator()
    {
        RuleFor(x => x.Type)
            .Equal(IdentityType.Microsoft);

        RuleFor(x => x.Value)
            .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
    }
}
