using FluentValidation;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Validators
{
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
}
