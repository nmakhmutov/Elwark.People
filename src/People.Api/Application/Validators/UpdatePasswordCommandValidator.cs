using FluentValidation;
using FluentValidation.Results;
using People.Api.Application.Commands;
using People.Api.Infrastructure.Password;

namespace People.Api.Application.Validators
{
    public sealed class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
    {
        public UpdatePasswordCommandValidator(IPasswordValidator validator)
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty();

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .CustomAsync(async (password, context, token) =>
                {
                    var (isSuccess, error) = await validator.ValidateAsync(password, token);
                    if (!isSuccess)
                        context.AddFailure(
                            new ValidationFailure(nameof(SignUpByEmailCommand.Password), "Incorrect password")
                            {
                                ErrorCode = error
                            }
                        );
                });
        }
    }
}
