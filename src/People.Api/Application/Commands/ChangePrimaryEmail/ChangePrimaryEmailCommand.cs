using System.Net.Mail;
using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

namespace People.Api.Application.Commands.ChangePrimaryEmail;

internal sealed record ChangePrimaryEmailCommand(long Id, MailAddress Email) : IRequest;

internal sealed class ChangePrimaryEmailCommandHandler : IRequestHandler<ChangePrimaryEmailCommand>
{
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public ChangePrimaryEmailCommandHandler(IAccountRepository repository, ITimeProvider time)
    {
        _repository = repository;
        _time = time;
    }

    public async Task Handle(ChangePrimaryEmailCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.SetPrimaryEmail(request.Email, _time);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
