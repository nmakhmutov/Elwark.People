using FluentValidation;
using FluentValidation.Results;
using People.Account.Api.Infrastructure.Password;

namespace People.Account.Api.Application.Commands.CreatePassword
{
    public sealed class CreatePasswordCommandValidator : AbstractValidator<CreatePasswordCommand>
    {
        public CreatePasswordCommandValidator(IPasswordValidator validator)
        {
            RuleFor(x => x.Password)
                .NotEmpty()
                .MaximumLength(Domain.Aggregates.AccountAggregate.Password.MaxLength)
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
    }
}
