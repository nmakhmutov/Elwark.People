using FluentValidation;
using People.Api.Application.Commands.Attach;
using People.Domain.Aggregates.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators.Attach
{
    public sealed class AttachEmailCommandValidator : AbstractValidator<AttachEmailCommand>
    {
        public AttachEmailCommandValidator(IAccountRepository repository) =>
            RuleFor(x => x.Email)
                .NotNull()
                .MustAsync(async (email, ct) => !await repository.IsExists(email, ct))
                .WithErrorCode(ElwarkExceptionCodes.EmailAlreadyExists);
    }
}
