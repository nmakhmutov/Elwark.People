using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Api.Infrastructure.Password;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class CreatePasswordCommandValidator : AbstractValidator<CreatePasswordCommand>
    {
        public CreatePasswordCommandValidator(IConfirmationService confirmation, IPasswordValidator validator)
        {
            RuleFor(x => x)
                .CustomAsync(async (command, context, ct) =>
                {
                    var data = await confirmation.GetAsync(command.ConfirmationId, ct);
                    if (data is null)
                    {
                        context.AddFailure(
                            new ValidationFailure(nameof(CreatePasswordCommand.ConfirmationCode), "Not found")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotFound
                            });

                        return;
                    }

                    if (data.Code != command.ConfirmationCode)
                        context.AddFailure(
                            new ValidationFailure(nameof(CreatePasswordCommand.ConfirmationCode), "Not match")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotMatch
                            });
                });

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
        }
    }
}
