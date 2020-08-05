using Elwark.People.Api.Extensions;
using Elwark.People.Shared.Primitives;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class UrlTemplateValidation : AbstractValidator<UrlTemplate>
    {
        public UrlTemplateValidation()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .Url()
                .Must((command, value) => value?.Contains(command.Marker) ?? false)
                .WithMessage("Url template must contain code marker");

            RuleFor(x => x.Marker)
                .NotEmpty();
        }
    }
}