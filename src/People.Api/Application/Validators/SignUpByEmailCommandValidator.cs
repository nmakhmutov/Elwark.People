using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Api.Infrastructure.Password;
using People.Domain.Exceptions;
using People.Infrastructure.Prohibitions;

namespace People.Api.Application.Validators
{
    public sealed class SignUpByEmailCommandValidator : AbstractValidator<SignUpByEmailCommand>
    {
        public SignUpByEmailCommandValidator(IPasswordValidator validator, IProhibitionService prohibitionService)
        {
            CascadeMode = CascadeMode.Stop;
            RuleFor(x => x.Password)
                .NotEmpty()
                .CustomAsync(async (password, context, token) =>
                {
                    try
                    {
                        await validator.ValidateAsync(password, token);
                    }
                    catch (ElwarkException ex)
                    {
                        var failure = new ValidationFailure(nameof(SignUpByEmailCommand.Password), "Incorrect password")
                        {
                            ErrorCode = ex.Code
                        };

                        context.AddFailure(failure);
                    }
                });

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
                .WithErrorCode(ElwarkExceptionCodes.EmailHostDenied);
        }
    }
}