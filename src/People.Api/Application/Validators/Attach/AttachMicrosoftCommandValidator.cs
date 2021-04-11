using FluentValidation;
using People.Api.Application.Commands.Attach;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Validators.Attach
{
    public sealed class AttachMicrosoftCommandValidator : AbstractValidator<AttachMicrosoftCommand>
    {
        public AttachMicrosoftCommandValidator(IAccountRepository repository) =>
            RuleFor(x => x.Microsoft)
                .NotNull()
                .MustAsync(async (google, ct) => !await repository.IsExists(google, ct))
                .WithErrorCode(ElwarkExceptionCodes.ProviderAlreadyExists);
    }
}
