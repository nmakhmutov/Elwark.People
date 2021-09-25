using FluentValidation;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Validators
{
    public sealed class IdentityGoogleValidator : AbstractValidator<Identity.Google>
    {
        public IdentityGoogleValidator()
        {
            RuleFor(x => x.Type)
                .Equal(IdentityType.Google);

            RuleFor(x => x.Value)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
