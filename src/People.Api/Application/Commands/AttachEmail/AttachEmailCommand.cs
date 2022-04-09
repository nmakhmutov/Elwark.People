using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.AttachEmail;

public sealed record AttachEmailCommand(AccountId Id, Identity.Email Email) : IRequest;

internal sealed class AttachEmailCommandHandler : IRequestHandler<AttachEmailCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public AttachEmailCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(AttachEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.AddEmail(request.Email, false, DateTime.UtcNow);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
