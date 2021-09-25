using System.Net.Mail;
using FluentValidation;
using FluentValidation.Results;
using People.Account.Api.Application.Validators;
using People.Account.Api.Infrastructure.Password;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Exceptions;
using People.Account.Infrastructure.Forbidden;

namespace People.Account.Api.Application.Commands.SignUpByEmail
{
    public sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
    {
        public SignUpByEmailCommandValidator(IAccountRepository repository, IPasswordValidator validator,
            IForbiddenService forbiddenService)
        {
            CascadeMode = CascadeMode.Stop;
            
            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .MaximumLength(Password.MaxLength)
                .CustomAsync(async (password, context, token) =>
                {
                    var (isSuccess, error) = await validator.ValidateAsync(password, token);
                    if (!isSuccess)
                        context.AddFailure(
                            new ValidationFailure(nameof(SignUpByEmailCommand.Password), "Incorrect password")
                            {
                                ErrorCode = error
                            }
                        );
                });

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignUpByEmailCommand.Email))
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists)
                .MustAsync(async (email, ct) =>
                {
                    var host = new MailAddress(email.Value).Host;
                    return !await forbiddenService.IsEmailHostDenied(host, ct);
                })
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied);
        }
    }
}
