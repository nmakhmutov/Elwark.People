using System.Net.Mail;
using MediatR;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.DeleteEmail;

internal sealed record DeleteEmailCommand(long Id, MailAddress Email) : IRequest;

internal sealed class DeleteEmailCommandHandler : IRequestHandler<DeleteEmailCommand>
{
    private readonly IAccountRepository _repository;

    public DeleteEmailCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task Handle(DeleteEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.DeleteEmail(request.Email);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
