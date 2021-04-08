using FluentValidation;
using People.Api.Application.Commands;

namespace People.Api.Application.Validators
{
    public sealed class SignInByMicrosoftCommandValidator : AbstractValidator<SignInByMicrosoftCommand>
    {
        public SignInByMicrosoftCommandValidator() =>
            RuleFor(x => x.Identity)
                .NotNull()
                .ChildRules(x =>
                    x.RuleFor(t => t.Value)
                        .NotEmpty()
                        .OverridePropertyName(nameof(SignInByMicrosoftCommand.Identity))
                );
    }
}
