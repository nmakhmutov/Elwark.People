using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

internal sealed class IdentityGoogleValidator : AbstractValidator<Identity.Google>
{
    public IdentityGoogleValidator()
    {
        RuleFor(x => x.Type)
            .Equal(IdentityType.Google);

        RuleFor(x => x.Value)
            .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
    }
}
