using FluentValidation;
using People.Api.Application.Commands.SignUp;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Validators.SignUp
{
    public sealed class SignUpByGoogleCommandValidator : AbstractValidator<SignUpByGoogleCommand>
    {
        public SignUpByGoogleCommandValidator(IAccountRepository repository, IForbiddenService forbiddenService)
        {
            CascadeMode = CascadeMode.Stop;
            RuleFor(x => x.Google)
                .NotNull()
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
                    return !await forbiddenService.IsEmailHostDenied(host, ct);
                })
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied)
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
        }
    }
}
