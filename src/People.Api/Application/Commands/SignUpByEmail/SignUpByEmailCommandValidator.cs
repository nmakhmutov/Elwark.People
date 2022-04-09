using System.Net.Mail;
using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Validators;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Blacklist;

namespace People.Api.Application.Commands.SignUpByEmail;

internal sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
{
    public SignUpByEmailCommandValidator(IAccountRepository repository, IPasswordValidator validator,
        IBlacklistService blacklist)
    {
        CascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
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
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SignUpByEmailCommand.Email))
            .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
            .WithErrorCode(ExceptionCodes.EmailAlreadyExists)
            .MustAsync(async (email, ct) =>
            {
                var host = new MailAddress(email.Value).Host;
                return !await blacklist.IsEmailHostDenied(host, ct);
            })
            .WithErrorCode(ExceptionCodes.EmailHostDenied);
    }
}
