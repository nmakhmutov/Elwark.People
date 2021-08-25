using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Api.Application.Commands.AttachEmail;
using People.Api.Application.Validators;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.AttachMicrosoft
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
