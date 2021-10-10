using FluentValidation;
using FluentValidation.Results;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Api.Application.Commands.UpdatePassword;

internal sealed class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
{
    public UpdatePasswordCommandValidator(IPasswordValidator validator)
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.OldPassword));

        RuleFor(x => x.NewPassword)
            .NotEmpty().OverridePropertyName(nameof(UpdatePasswordCommand.NewPassword))
            .MaximumLength(Password.MaxLength)
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
