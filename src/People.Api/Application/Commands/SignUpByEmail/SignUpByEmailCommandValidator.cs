using System.Net.Mail;
using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Validators;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Commands.SignUpByEmail;

internal sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
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
