using FluentValidation;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.ConfirmConnection
{
    public sealed class ConfirmConnectionCommandValidator : AbstractValidator<ConfirmConnectionCommand>
    {
        public ConfirmConnectionCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
