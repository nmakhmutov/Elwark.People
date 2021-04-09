using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Api.Infrastructure.Password;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Validators
{
    public sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
    {
        public SignUpByEmailCommandValidator(IAccountRepository repository, IPasswordValidator validator,
            IForbiddenService forbiddenService)
        {
            CascadeMode = CascadeMode.Stop;
            RuleFor(x => x.Password)
                .NotEmpty()
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
                .ChildRules(x => x.RuleFor(t => t.Value)
                    .NotEmpty()
                    .EmailAddress()
                    .WithErrorCode(ElwarkExceptionCodes.EmailIncorrectFormat)
                )
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists)
                .MustAsync(async (email, ct) =>
                {
                    var host = email.GetMailAddress().Host;
                    return !await forbiddenService.IsEmailHostDenied(host, ct);
                })
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied);
        }
    }
}
