using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Account.Api.Application.Commands.AttachEmail;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.AttachGoogle
{
    public sealed class AttachGoogleCommandValidator : AbstractValidator<AttachGoogleCommand>
    {
        public AttachGoogleCommandValidator(IAccountRepository repository)
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            async Task<bool> BeUnique(Identity.Google google, CancellationToken ct) =>
                !await repository.IsExists(google, ct);

            RuleFor(x => x.Google)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityGoogleValidator()).OverridePropertyName(nameof(AttachGoogleCommand.Email))
                .MustAsync(BeUnique).WithErrorCode(ElwarkExceptionCodes.ConnectionAlreadyExists).OverridePropertyName(nameof(AttachGoogleCommand.Email));

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AttachEmailCommand.Email));
        }
    }
}
