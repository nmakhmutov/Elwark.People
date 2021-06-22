using FluentValidation;
using People.Api.Application.Commands.Attach;
using People.Domain.Aggregates.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators.Attach
{
    public sealed class AttachGoogleCommandValidator : AbstractValidator<AttachGoogleCommand>
    {
        public AttachGoogleCommandValidator(IAccountRepository repository) =>
            RuleFor(x => x.Google)
                .NotNull()
                .MustAsync(async (google, ct) => !await repository.IsExists(google, ct))
                .WithErrorCode(ElwarkExceptionCodes.ProviderAlreadyExists);
    }
}
