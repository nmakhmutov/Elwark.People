using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record DeleteIdentityCommand(AccountId Id, Identity Identity) : IRequest;

    internal sealed class DeleteIdentityCommandHandler : IRequestHandler<DeleteIdentityCommand>
    {
        private readonly IAccountRepository _repository;

        public DeleteIdentityCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(DeleteIdentityCommand request, CancellationToken ct)
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
