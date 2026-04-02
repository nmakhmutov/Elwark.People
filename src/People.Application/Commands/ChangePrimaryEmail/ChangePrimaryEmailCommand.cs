using System.Net.Mail;
using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ChangePrimaryEmail;

public sealed record ChangePrimaryEmailCommand(AccountId Id, MailAddress Email) : ICommand;

public sealed class ChangePrimaryEmailCommandHandler : ICommandHandler<ChangePrimaryEmailCommand>
{
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangePrimaryEmailCommandHandler(IAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Unit> Handle(ChangePrimaryEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.SetPrimaryEmail(request.Email, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
