using System;
using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class SendPrimaryEmailConfirmationCommandValidator 
        : AbstractValidator<SendPrimaryEmailConfirmationCommand>
    {
        public SendPrimaryEmailConfirmationCommandValidator(IConfirmationService confirmation)
        {
            RuleFor(x => x.Id)
                .NotNull()
                .CustomAsync(async (id, context, ct) =>
                {
                    var data = await confirmation.GetSignUpConfirmation(id, ct);
                    if (data is null)
                        return;

                    if ((DateTime.UtcNow - data.CreatedAt).TotalMinutes > 1)
                        return;

                    var failure = new ValidationFailure(nameof(SendPrimaryEmailConfirmationCommand.Email),
                        "Confirmation already sent")
                    {
                        ErrorCode = ElwarkExceptionCodes.ConfirmationAlreadySent
                    };

                    context.AddFailure(failure);
                });
        }
    }
}