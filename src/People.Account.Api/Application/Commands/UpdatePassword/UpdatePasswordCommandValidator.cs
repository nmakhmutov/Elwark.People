using FluentValidation;
using FluentValidation.Results;
using People.Account.Api.Infrastructure.Password;

namespace People.Account.Api.Application.Commands.UpdatePassword
{
    public sealed class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
    {
        public UpdatePasswordCommandValidator(IPasswordValidator validator)
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.OldPassword));

            RuleFor(x => x.NewPassword)
                .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.NewPassword))
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
