using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Blacklist;

namespace People.Api.Application.Commands.AttachEmail;

internal sealed class AttachEmailCommandValidator : AbstractValidator<AttachEmailCommand>
{
    public AttachEmailCommandValidator(IAccountRepository repository, IBlacklistService blacklist)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required);

        async Task<bool> BeUniqueEmail(EmailIdentity email, CancellationToken ct) =>
            !await repository.IsExists(email, ct);

        async Task<bool> BeAllowed(EmailIdentity email, CancellationToken ct) =>
            !await blacklist.IsEmailHostDenied(new MailAddress(email.Value).Host, ct);

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(AttachEmailCommand.Email))
            .MustAsync(BeAllowed).WithErrorCode(ExceptionCodes.EmailHostDenied)
            .MustAsync(BeUniqueEmail).WithErrorCode(ExceptionCodes.EmailAlreadyExists);
    }
}
