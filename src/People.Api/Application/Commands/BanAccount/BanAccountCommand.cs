using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.BanAccount;

internal sealed record BanAccountCommand(AccountId Id, string Reason, DateTime? ExpiredAt) : IRequest;

internal sealed class BanAccountCommandHandler : IRequestHandler<BanAccountCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public BanAccountCommandHandler(IMediator mediator, IAccountRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    public async Task<Unit> Handle(BanAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

        if (request.ExpiredAt.HasValue)
            account.SetTemporaryBan(request.Reason, request.ExpiredAt.Value, DateTime.UtcNow);
        else
            account.SetPermanentBan(request.Reason, DateTime.UtcNow);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
