using System;
using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SendConfirmation;

internal sealed class SendConfirmationCommandValidator : AbstractValidator<SendConfirmationCommand>
{
    public SendConfirmationCommandValidator(IConfirmationService confirmation)
    {
        RuleFor(x => x.Id)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .MustAsync(async (id, ct) =>
            {
                var data = await confirmation.GetAsync(id, ct);
                if (data is null)
                    return true;

                return (DateTime.UtcNow - data.CreatedAt).TotalMinutes > 2;
            })
            .WithErrorCode(ExceptionCodes.ConfirmationAlreadySent);

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SendConfirmationCommand.Email));
    }
}
