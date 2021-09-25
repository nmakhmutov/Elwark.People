using FluentValidation;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Validators
{
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
}
