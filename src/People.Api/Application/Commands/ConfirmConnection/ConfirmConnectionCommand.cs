using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmConnection
{
    public sealed record ConfirmConnectionCommand(
        AccountId Id,
        ObjectId ConfirmationId,
        uint ConfirmationCode,
        Identity? Identity = null
    ) : IRequest;

    internal sealed class ConfirmConnectionCommandHandler : IRequestHandler<ConfirmConnectionCommand>
    {
        private readonly IConfirmationService _confirmation;
        private readonly IAccountRepository _repository;

        public ConfirmConnectionCommandHandler(IAccountRepository repository, IConfirmationService confirmation)
        {
            _repository = repository;
            _confirmation = confirmation;
        }

        public async Task<Unit> Handle(ConfirmConnectionCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var identity = request.Identity ?? account.GetPrimaryEmail().Identity;

            account.ConfirmConnection(identity, DateTime.UtcNow);

            await _repository.UpdateAsync(account, ct);
            await _confirmation.DeleteAsync(account.Id, ct);

            return Unit.Value;
        }
    }
}
