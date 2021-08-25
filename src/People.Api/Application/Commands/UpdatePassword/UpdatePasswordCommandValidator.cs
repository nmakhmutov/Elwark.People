using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands.SignUpByEmail;
using People.Api.Infrastructure.Password;

namespace People.Api.Application.Commands.UpdatePassword
{
    public sealed class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
    {
        public UpdatePasswordCommandValidator(IPasswordValidator validator)
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.OldPassword));

            RuleFor(x => x.NewPassword)
                .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.OldPassword))
                .MaximumLength(Domain.Aggregates.AccountAggregate.Password.MaxLength)
                .CustomAsync(async (password, context, token) =>
                {
                    var (isSuccess, error) = await validator.ValidateAsync(password, token);
                    if (!isSuccess)
                        context.AddFailure(
                            new ValidationFailure(nameof(UpdatePasswordCommand.NewPassword), "Incorrect password")
                            {
                                ErrorCode = error
                            }
                        );
                });
        }
    }
}
