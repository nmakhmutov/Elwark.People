using System;
using FluentValidation;
using People.Account.Api.Application.Validators;
using People.Account.Domain.Exceptions;
using People.Account.Infrastructure.Confirmations;

namespace People.Account.Api.Application.Commands.SendConfirmation
{
    public sealed class SendConfirmationCommandValidator : AbstractValidator<SendConfirmationCommand>
    {
        public SendConfirmationCommandValidator(IConfirmationService confirmation)
        {
            RuleFor(x => x.Id)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .MustAsync(async (id, ct) =>
                {
                    var data = await confirmation.GetAsync(id, ct);
                    if (data is null)
                        return true;

                    return (DateTime.UtcNow - data.CreatedAt).TotalMinutes > 2;
                })
                .WithErrorCode(ElwarkExceptionCodes.ConfirmationAlreadySent);

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SendConfirmationCommand.Email));
        }
    }
}
