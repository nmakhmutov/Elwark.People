using FluentValidation;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators
{
    public sealed class IdentityGoogleValidator : AbstractValidator<Identity.Google>
    {
        public IdentityGoogleValidator()
        {
            RuleFor(x => x.Type)
                .Equal(Connection.Type.Google);

            RuleFor(x => x.Value)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
