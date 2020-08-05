using Elwark.People.Api.Application.Commands.AttachIdentity;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class AttachEmailIdentityCommandValidator : AbstractValidator<AttachEmailIdentityCommand>
    {
        public AttachEmailIdentityCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotNull()
                .GreaterThan(0);
            
            RuleFor(x => x.Email)
                .NotNull();
        }
    }
}