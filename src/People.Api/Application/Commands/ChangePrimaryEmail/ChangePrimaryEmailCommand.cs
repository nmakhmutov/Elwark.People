using System.Net.Mail;
using MediatR;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.ChangePrimaryEmail;

internal sealed record ChangePrimaryEmailCommand(long Id, MailAddress Email) : IRequest;

internal sealed class ChangePrimaryEmailCommandHandler : IRequestHandler<ChangePrimaryEmailCommand>
{
    private readonly IAccountRepository _repository;

    public ChangePrimaryEmailCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task Handle(ChangePrimaryEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.SetPrimaryEmail(request.Email);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
