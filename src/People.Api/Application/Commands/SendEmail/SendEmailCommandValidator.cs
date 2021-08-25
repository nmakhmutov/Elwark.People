using FluentValidation;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SendEmail
{
    public sealed class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
    {
        public SendEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required);

            RuleFor(x => x.Subject)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);

            RuleFor(x => x.Body)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required);
        }
    }
}
