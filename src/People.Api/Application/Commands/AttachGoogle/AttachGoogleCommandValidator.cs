using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Api.Application.Commands.AttachEmail;
using People.Api.Application.Validators;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.AttachGoogle;

internal sealed class AttachGoogleCommandValidator : AbstractValidator<AttachGoogleCommand>
{
    public AttachGoogleCommandValidator(IAccountRepository repository)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        async Task<bool> BeUnique(GoogleIdentity google, CancellationToken ct) =>
            !await repository.IsExists(google, ct);

        RuleFor(x => x.Google)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityGoogleValidator()).OverridePropertyName(nameof(AttachGoogleCommand.Email))
            .MustAsync(BeUnique).WithErrorCode(ExceptionCodes.ConnectionAlreadyExists)
            .OverridePropertyName(nameof(AttachGoogleCommand.Email));

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AttachEmailCommand.Email));
    }
}
