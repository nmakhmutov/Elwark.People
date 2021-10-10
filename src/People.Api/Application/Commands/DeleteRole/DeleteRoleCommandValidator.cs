using FluentValidation;

namespace People.Api.Application.Commands.DeleteRole;

internal sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty();
    }
}
