using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.ConfirmConnection
{
    public sealed record ConfirmConnectionCommand(AccountId Id, Identity? Identity = null) : IRequest;

    internal sealed class ConfirmConnectionCommandHandler : IRequestHandler<ConfirmConnectionCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public ConfirmConnectionCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(ConfirmConnectionCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var identity = request.Identity ?? account.GetPrimaryEmail().Identity;

            account.ConfirmConnection(identity, DateTime.UtcNow);

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }
    }
}
