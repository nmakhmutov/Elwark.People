using Elwark.People.Api.Application.Commands.AttachIdentity;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class AttachFacebookIdentityCommandValidator : AbstractValidator<AttachFacebookIdentityCommand>
    {
        public AttachFacebookIdentityCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.AccessToken)
                .NotEmpty();
        }
    }
}