using FluentValidation;
using People.Api.Application.Commands;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Validators
{
    public sealed class SignUpByMicrosoftCommandValidator : AbstractValidator<SignUpByMicrosoftCommand>
    {
        public SignUpByMicrosoftCommandValidator(IAccountRepository repository, IForbiddenService forbiddenService)
        {
            CascadeMode = CascadeMode.Stop;
            RuleFor(x => x.Identity)
                .NotEmpty()
                .MustAsync(async (microsoft, ct) => !await repository.IsExists(microsoft, ct))
                .WithErrorCode(ElwarkExceptionCodes.ProviderAlreadyExists);

            RuleFor(x => x.Email)
                .ChildRules(x => x.RuleFor(t => t.Value)
                    .NotEmpty()
                    .EmailAddress()
                    .WithErrorCode(ElwarkExceptionCodes.EmailIncorrectFormat)
                )
                .MustAsync(async (email, ct) =>
                {
                    var host = email.GetMailAddress().Host;
                    return !await forbiddenService.IsEmailHostDenied(host, ct);
                })
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied)
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
        }
    }
}