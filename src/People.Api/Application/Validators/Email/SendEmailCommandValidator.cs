using FluentValidation;
using People.Api.Application.Commands.Email;

namespace People.Api.Application.Validators.Email
{
    public sealed class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
    {
        public SendEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotNull();

            RuleFor(x => x.Subject)
                .NotEmpty();

            RuleFor(x => x.Body)
                .NotEmpty();
        }
    }
}
