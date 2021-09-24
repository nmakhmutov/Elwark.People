using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.DeleteConnection
{
    public sealed record DeleteConnectionCommand(AccountId Id, Identity Identity) : IRequest;

    internal sealed class DeleteConnectionCommandHandler : IRequestHandler<DeleteConnectionCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public DeleteConnectionCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(DeleteConnectionCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.DeleteIdentity(request.Identity);

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }
    }
}
