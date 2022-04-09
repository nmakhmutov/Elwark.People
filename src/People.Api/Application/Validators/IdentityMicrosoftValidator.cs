using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators;

internal sealed class IdentityMicrosoftValidator : AbstractValidator<MicrosoftIdentity>
{
    public IdentityMicrosoftValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required);
    }
}
