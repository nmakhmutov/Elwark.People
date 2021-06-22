using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Domain.Aggregates.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Queries
{
    public sealed record CheckSignUpConfirmationQuery(ObjectId ConfirmationId) : IRequest<Confirmation>;

    public sealed record CheckSignUpConfirmationQueryHandler
        : IRequestHandler<CheckSignUpConfirmationQuery, Confirmation>
    {
        private readonly IConfirmationService _confirmation;
        private readonly IAccountRepository _repository;

        public CheckSignUpConfirmationQueryHandler(IAccountRepository repository, IConfirmationService confirmation)
        {
            _repository = repository;
            _confirmation = confirmation;
        }

        public async Task<Confirmation> Handle(CheckSignUpConfirmationQuery request,
            CancellationToken ct)
        {
            var confirmation = await _confirmation.GetAsync(request.ConfirmationId, ct);
            if (confirmation is null)
                throw new ElwarkException(ElwarkExceptionCodes.ConfirmationNotFound);

            var account = await _repository.GetAsync(confirmation.AccountId, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (account.IsConfirmed())
                throw new ElwarkException(ElwarkExceptionCodes.IdentityAlreadyConfirmed);

            return confirmation;
        }
    }
}
