using System;
using FluentValidation;
using People.Api.Application.Commands;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class SendConfirmationCommandValidator : AbstractValidator<SendConfirmationCommand>
    {
        public SendConfirmationCommandValidator(IConfirmationService confirmation) =>
            RuleFor(x => x.Id)
                .NotNull()
                .MustAsync(async (id, ct) =>
                {
                    var data = await confirmation.GetAsync(id, ct);
                    if (data is null)
                        return true;

                    return (DateTime.UtcNow - data.CreatedAt).TotalMinutes > 2;
                })
                .WithErrorCode(ElwarkExceptionCodes.ConfirmationAlreadySent);
    }
}
