using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Commands.SignUpByGoogle;

internal sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
{
    public SignUpByGoogleCommandValidator(IAccountRepository repository, IForbiddenService forbiddenService)
    {
        CascadeMode = CascadeMode.Stop;

        async Task<bool> BeUnique(Identity identity, CancellationToken ct) =>
            !await repository.IsExists(identity, ct);

        async Task<bool> BeAllowed(Identity.Email email, CancellationToken ct) =>
            !await forbiddenService.IsEmailHostDenied(new MailAddress(email.Value).Host, ct);

        RuleFor(x => x.Google)
            .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
            .MustAsync(BeUnique).WithErrorCode(ElwarkExceptionCodes.ConnectionAlreadyExists);

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignUpByGoogleCommand.Email))
            .MustAsync(BeAllowed).WithErrorCode(ElwarkExceptionCodes.EmailHostDenied)
            .MustAsync(BeUnique).WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
    }
}
