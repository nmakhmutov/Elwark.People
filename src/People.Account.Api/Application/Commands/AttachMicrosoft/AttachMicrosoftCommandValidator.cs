using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Account.Api.Application.Commands.AttachEmail;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.AttachMicrosoft
{
    public sealed class AttachMicrosoftCommandValidator : AbstractValidator<AttachMicrosoftCommand>
    {
        public AttachMicrosoftCommandValidator(IAccountRepository repository)
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            async Task<bool> BeUnique(Identity.Microsoft google, CancellationToken ct) =>
                !await repository.IsExists(google, ct);

            RuleFor(x => x.Microsoft)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityMicrosoftValidator()).OverridePropertyName(nameof(AttachMicrosoftCommand.Email))
                .MustAsync(BeUnique).OverridePropertyName(nameof(AttachMicrosoftCommand.Email)).WithErrorCode(ElwarkExceptionCodes.ConnectionAlreadyExists);

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AttachEmailCommand.Email));
        }
    }
}
