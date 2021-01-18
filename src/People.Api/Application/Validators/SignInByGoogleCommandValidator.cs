using FluentValidation;
using People.Api.Application.Commands;

namespace People.Api.Application.Validators
{
    public sealed class SignInByGoogleCommandValidator : AbstractValidator<SignInByGoogleCommand>
    {
        public SignInByGoogleCommandValidator()
        {
            RuleFor(x => x.Identity)
                .NotNull()
                .ChildRules(x =>
                    x.RuleFor(t => t.Value)
                        .NotEmpty()
                        .OverridePropertyName(nameof(SignInByGoogleCommand.Identity))
                );
        }
    }
}