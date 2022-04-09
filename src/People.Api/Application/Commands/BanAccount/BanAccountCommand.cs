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
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.SetBan(request.Reason, request.ExpiredAt ?? new DateTime(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
