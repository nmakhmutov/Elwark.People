using FluentValidation;
using People.Api.Application.Commands;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Prohibitions;

namespace People.Api.Application.Validators
{
    public sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
    {
        public SignUpByGoogleCommandValidator(IAccountRepository repository, IProhibitionService prohibitionService)
        {
            CascadeMode = CascadeMode.Stop;
            RuleFor(x => x.Identity)
                .NotEmpty()
                .MustAsync(async (google, ct) => !await repository.IsExists(google, ct))
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
                    return !await prohibitionService.IsEmailHostDenied(host, ct);
                })
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied)
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
        }
    }
}