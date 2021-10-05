using FluentValidation;
using People.Api.Application.Validators;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SetAsPrimaryEmail;

internal sealed class SetAsPrimaryEmailCommandValidator : AbstractValidator<SetAsPrimaryEmailCommand>
{
    public SetAsPrimaryEmailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotNull().WithErrorCode(ElwarkExceptionCodes.Required)
            .SetValidator(new IdentityEmailValidator()).OverridePropertyName(nameof(SetAsPrimaryEmailCommand.Email));
    }
}
