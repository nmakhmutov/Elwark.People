using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class ConfirmEmailSignUpCommandValidator : AbstractValidator<ConfirmEmailSignUpCommand>
    {
        public ConfirmEmailSignUpCommandValidator(IConfirmationService confirmation)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .CustomAsync(async (code, context, ct) =>
                {
                    var id = (context.InstanceToValidate as ConfirmEmailSignUpCommand)!.Id;
                    var data = await confirmation.GetSignUpConfirmation(id, ct);
                    if (data is null)
                    {
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmEmailSignUpCommand.Code), "Confirmation not found")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotFound
                            });

                        return;
                    }

                    if (data.Code != code)
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmEmailSignUpCommand.Code), "Confirmation not match")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotMatch
                            });
                });
        }
    }
}