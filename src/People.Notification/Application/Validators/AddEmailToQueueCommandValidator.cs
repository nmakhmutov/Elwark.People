using FluentValidation;
using People.Notification.Application.Commands;

namespace People.Notification.Application.Validators
{
    public sealed class AddEmailToQueueCommandValidator : AbstractValidator<AddEmailToQueueCommand>
    {
        public AddEmailToQueueCommandValidator()
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