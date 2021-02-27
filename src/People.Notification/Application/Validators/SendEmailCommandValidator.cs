using FluentValidation;
using People.Notification.Application.Commands;

namespace People.Notification.Application.Validators
{
    public sealed class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
    {
        public SendEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Subject)
                .NotEmpty();

            RuleFor(x => x.Body)
                .NotEmpty();
        }
    }
}