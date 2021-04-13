using FluentValidation;
using People.Api.Application.Commands.Email;

namespace People.Api.Application.Validators.Email
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
