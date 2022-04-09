using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

internal sealed class IdentityGoogleValidator : AbstractValidator<GoogleIdentity>
{
    public IdentityGoogleValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required);
    }
}
