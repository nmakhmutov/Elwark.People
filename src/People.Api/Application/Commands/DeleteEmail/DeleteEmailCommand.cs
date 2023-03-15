using System.Net.Mail;
using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

namespace People.Api.Application.Commands.DeleteEmail;

internal sealed record DeleteEmailCommand(long Id, MailAddress Email) : IRequest;

internal sealed class DeleteEmailCommandHandler : IRequestHandler<DeleteEmailCommand>
{
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public DeleteEmailCommandHandler(IAccountRepository repository, ITimeProvider time)
    {
        _repository = repository;
        _time = time;
    }

    public async Task Handle(DeleteEmailCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.DeleteEmail(request.Email, _time);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
