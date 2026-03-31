using System.Net.Mail;
using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.DeleteEmail;

internal sealed record DeleteEmailCommand(AccountId Id, MailAddress Email) : IRequest;

internal sealed class DeleteEmailCommandHandler : IRequestHandler<DeleteEmailCommand>
{
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeleteEmailCommandHandler(IAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Unit> Handle(DeleteEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.DeleteEmail(request.Email, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
