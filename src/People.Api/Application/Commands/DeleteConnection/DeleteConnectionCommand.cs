using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.DeleteConnection
{
    public sealed record DeleteConnectionCommand(AccountId Id, Identity Identity) : IRequest;

    internal sealed class DeleteConnectionCommandHandler : IRequestHandler<DeleteConnectionCommand>
    {
        private readonly IAccountRepository _repository;

        public DeleteConnectionCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(DeleteConnectionCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.DeleteIdentity(request.Identity);

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
