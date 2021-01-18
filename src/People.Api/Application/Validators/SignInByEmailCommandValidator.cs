using FluentValidation;
using People.Api.Application.Commands;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators
{
    public sealed class SignInByEmailCommandValidator : AbstractValidator<SignInByEmailCommand>
    {
        public SignInByEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .ChildRules(x =>
                    x.RuleFor(t => t.Value)
                        .NotEmpty()
                        .EmailAddress()
                        .WithErrorCode(ElwarkExceptionCodes.EmailIncorrectFormat)
                        .OverridePropertyName(nameof(SignInByEmailCommand.Email))
                );

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}