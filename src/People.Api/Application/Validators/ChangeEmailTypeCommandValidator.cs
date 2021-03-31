using FluentValidation;
using People.Api.Application.Commands;

namespace People.Api.Application.Validators
{
    public sealed class ChangeEmailTypeCommandValidator : AbstractValidator<ChangeEmailTypeCommand>
    {
        public ChangeEmailTypeCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Type)
                .IsInEnum();
        }
    }
}
