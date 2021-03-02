using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Validators
{
    public sealed class ConfirmIdentityCommandValidator : AbstractValidator<ConfirmIdentityCommand>
    {
        public ConfirmIdentityCommandValidator(IConfirmationService confirmation) =>
            RuleFor(x => x)
                .CustomAsync(async (command, context, ct) =>
                {
                    var data = await confirmation.GetAsync(command.ConfirmationId, ct);
                    if (data is null)
                    {
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmIdentityCommand.ConfirmationCode), "Not found")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotFound
                            });

                        return;
                    }

                    if (data.Code != command.ConfirmationCode)
                        context.AddFailure(
                            new ValidationFailure(nameof(ConfirmIdentityCommand.ConfirmationCode), "Not match")
                            {
                                ErrorCode = ElwarkExceptionCodes.ConfirmationNotMatch
                            });
                });
    }
}
