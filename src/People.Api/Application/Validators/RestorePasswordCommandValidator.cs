using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Api.Infrastructure.Password;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class RestorePasswordCommandValidator : AbstractValidator<RestorePasswordCommand>
    {
        public RestorePasswordCommandValidator(IConfirmationService confirmation, IPasswordValidator validator)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .CustomAsync(async (code, context, ct) =>
                {
                    var id = (context.InstanceToValidate as RestorePasswordCommand)!.Id;
                    var data = await confirmation.GetResetPasswordConfirmation(id, ct);
                    if (data is null)
                    {
                        context.AddFailure(
                            new ValidationFailure(nameof(RestorePasswordCommand.Code), "Confirmation not found")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotFound
                            });

                        return;
                    }

                    if (data.Code != code)
                        context.AddFailure(
                            new ValidationFailure(nameof(RestorePasswordCommand.Code), "Confirmation not match")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotMatch
                            });
                });
                
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
        }
    }
}