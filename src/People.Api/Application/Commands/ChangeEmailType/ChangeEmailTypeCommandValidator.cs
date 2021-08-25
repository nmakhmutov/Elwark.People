using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ChangeEmailType
{
    public sealed class ChangeEmailTypeCommandValidator : AbstractValidator<ChangeEmailTypeCommand>
    {
        public ChangeEmailTypeCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Email)
                .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
                .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(ChangeEmailTypeCommand.Email));

            RuleFor(x => x.Type)
                .IsInEnum();
        }
    }
}
