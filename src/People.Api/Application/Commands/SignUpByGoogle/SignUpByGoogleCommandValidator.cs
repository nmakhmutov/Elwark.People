using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Blacklist;

namespace People.Api.Application.Commands.SignUpByGoogle;

internal sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
{
    public SignUpByGoogleCommandValidator(IAccountRepository repository, IBlacklistService blacklist)
    {
        CascadeMode = CascadeMode.Stop;

        async Task<bool> BeUnique(Identity identity, CancellationToken ct) =>
            !await repository.IsExists(identity, ct);

        async Task<bool> BeAllowed(EmailIdentity email, CancellationToken ct) =>
            !await blacklist.IsEmailHostDenied(new MailAddress(email.Value).Host, ct);

        RuleFor(x => x.Google)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .MustAsync(BeUnique).WithErrorCode(ExceptionCodes.ConnectionAlreadyExists);

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignUpByGoogleCommand.Email))
            .MustAsync(BeAllowed).WithErrorCode(ExceptionCodes.EmailHostDenied)
            .MustAsync(BeUnique).WithErrorCode(ExceptionCodes.EmailAlreadyExists);
    }
}
