using Elwark.People.Api.Application.Queries;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class CheckConfirmationQueryValidator : AbstractValidator<CheckConfirmationQuery>
    {
        public CheckConfirmationQueryValidator()
        {
            RuleFor(t => t.Code)
                .NotEmpty()
                .GreaterThan(0);
                    
            RuleFor(t => t.Type)
                .NotEmpty()
                .IsInEnum();

            RuleFor(t => t.IdentityId)
                .NotEmpty();
        }
    }
}