using Mediator;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token, string Code) : ICommand<EmailAccount>;

public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, EmailAccount>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmEmailCommandHandler(
        IConfirmationService confirmation,
        TimeProvider timeProvider,
        IAccountRepository repository
    )
    {
        _confirmation = confirmation;
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async ValueTask<EmailAccount> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation.VerifyEmailAsync(request.Token, request.Code, ct);

        var account = await _repository.GetAsync(confirmation.AccountId, ct)
            ?? throw AccountException.NotFound(confirmation.AccountId);

        account.ConfirmEmail(confirmation.Email, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return account.Emails
            .First(x => x.Email == confirmation.Email.Address);
    }
}
