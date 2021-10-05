using FluentValidation;
using FluentValidation.Results;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Api.Application.Commands.CreatePassword;

public sealed class CreatePasswordCommandValidator : AbstractValidator<CreatePasswordCommand>
{
    public CreatePasswordCommandValidator(IPasswordValidator validator) =>
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(Password.MaxLength)
            .CustomAsync(async (password, context, token) =>
            {
                var (isSuccess, error) = await validator.ValidateAsync(password, token);
                if (!isSuccess)
                    context.AddFailure(
                        new ValidationFailure(nameof(CreatePasswordCommand.Password), "Incorrect password")
                        {
                            ErrorCode = error
                        }
                    );
            });
}
