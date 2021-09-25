using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.AttachEmail
{
    public sealed class AttachEmailCommandValidator : AbstractValidator<AttachEmailCommand>
    {
        public AttachEmailCommandValidator(IAccountRepository repository)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);

            async Task<bool> BeUniqueEmail(Identity.Email email, CancellationToken ct) =>
                !await repository.IsExists(email, ct);

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AttachEmailCommand.Email))
                .MustAsync(BeUniqueEmail).WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
        }
    }
}
