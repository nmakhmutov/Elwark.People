using FluentValidation;
using FluentValidation.Results;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmConnection
{
    public sealed class ConfirmConnectionCommandValidator : AbstractValidator<ConfirmConnectionCommand>
    {
        public ConfirmConnectionCommandValidator(IConfirmationService confirmation)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
            
            RuleFor(x => new { x.ConfirmationId, x.ConfirmationCode })
                .CustomAsync(async (command, context, ct) =>
                {
                    var data = await confirmation.GetAsync(command.ConfirmationId, ct);
                    if (data is null)
                    {
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmConnectionCommand.ConfirmationCode), "Not found")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotFound
                            });

                        return;
                    }

                    if (data.Code != command.ConfirmationCode)
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmConnectionCommand.ConfirmationCode), "Not match")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotMatch
                            });
                });
        }
    }
}
