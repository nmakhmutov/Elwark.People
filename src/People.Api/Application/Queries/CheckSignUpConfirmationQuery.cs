using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Queries
{
    public sealed record CheckSignUpConfirmationQuery(AccountId Id) : IRequest<CheckSignUpConfirmationResult>;

    public sealed record CheckSignUpConfirmationQueryHandler
        : IRequestHandler<CheckSignUpConfirmationQuery, CheckSignUpConfirmationResult>
    {
        private readonly IConfirmationService _confirmation;
        private readonly IAccountRepository _repository;

        public CheckSignUpConfirmationQueryHandler(IAccountRepository repository, IConfirmationService confirmation)
        {
            _repository = repository;
            _confirmation = confirmation;
        }

        public async Task<CheckSignUpConfirmationResult> Handle(CheckSignUpConfirmationQuery request,
            CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null) 
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (account.IsConfirmed()) 
                throw new ElwarkException(ElwarkExceptionCodes.IdentityAlreadyConfirmed);

            var confirmation = await _confirmation.GetSignUpConfirmation(request.Id, ct);
            if (confirmation is null) 
                throw new ElwarkException(ElwarkExceptionCodes.ConfirmationNotFound);

            var email = account.GetPrimaryEmail();

            return new CheckSignUpConfirmationResult(email.Key(), confirmation.CreatedAt, confirmation.ExpireAt);
        }
    }
    
    public sealed record CheckSignUpConfirmationResult(Identity Key, DateTime CreatedAt, DateTime ExpireAt);
}