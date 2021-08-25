using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands.SignUpByEmail;
using People.Api.Infrastructure.Password;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.CreatePassword
{
    public sealed class CreatePasswordCommandValidator : AbstractValidator<CreatePasswordCommand>
    {
        public CreatePasswordCommandValidator(IConfirmationService confirmation, IPasswordValidator validator)
        {
            RuleFor(x => new { x.ConfirmationId, x.ConfirmationCode })
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
                .MaximumLength(Domain.Aggregates.AccountAggregate.Password.MaxLength)
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
