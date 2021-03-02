using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands
{
    public sealed record ConfirmIdentityCommand(
        AccountId Id,
        ObjectId ConfirmationId,
        uint ConfirmationCode,
        Identity? Identity = null
    ) : IRequest;

    internal sealed class ConfirmIdentityCommandHandler : IRequestHandler<ConfirmIdentityCommand>
    {
        private readonly IConfirmationService _confirmation;
        private readonly IAccountRepository _repository;

        public ConfirmIdentityCommandHandler(IAccountRepository repository, IConfirmationService confirmation)
        {
            _repository = repository;
            _confirmation = confirmation;
        }

        public async Task<Unit> Handle(ConfirmIdentityCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var identity = request.Identity ?? account.GetPrimaryEmail().GetIdentity();

            account.ConfirmIdentity(identity, DateTime.UtcNow);

            await _repository.UpdateAsync(account, ct);
            await _confirmation.DeleteAsync(account.Id, ct);

            return Unit.Value;
        }
    }
}
