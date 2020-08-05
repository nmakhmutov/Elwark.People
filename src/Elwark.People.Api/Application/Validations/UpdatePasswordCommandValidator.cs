using Elwark.People.Api.Application.Commands;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
    {
        public UpdatePasswordCommandValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .GreaterThan(0);

            RuleFor(x => x.NewPassword)
                .NotEmpty();
        }
    }
}