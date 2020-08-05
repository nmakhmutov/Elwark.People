using Elwark.People.Api.Application.Commands;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class SendConfirmationUrlCommandValidator : AbstractValidator<SendConfirmationUrlCommand>
    {
        public SendConfirmationUrlCommandValidator()
        {
            RuleFor(x => x.ConfirmationType)
                .NotEmpty()
                .IsInEnum();

            RuleFor(x => x.IdentityId)
                .NotEmpty();

            RuleFor(x => x.Notification)
                .NotEmpty();

            RuleFor(x => x.UrlTemplate)
                .NotEmpty()
                .SetValidator(new UrlTemplateValidation());
        }
    }
}