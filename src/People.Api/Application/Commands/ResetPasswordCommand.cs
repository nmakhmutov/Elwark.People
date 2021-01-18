using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands
{
    public sealed record ResetPasswordCommand(Identity Key) : IRequest<AccountId>;

    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AccountId>
    {
        private readonly IAccountRepository _repository;
        private readonly IConfirmationService _confirmation;

        public ResetPasswordCommandHandler(IAccountRepository repository, IConfirmationService confirmation)
        {
            _repository = repository;
            _confirmation = confirmation;
        }

        public async Task<AccountId> Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Key, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (!account.IsPasswordAvailable())
                throw new ElwarkException(ElwarkExceptionCodes.PasswordNotCreated);

            var confirmation = await _confirmation.GetResetPasswordConfirmation(account.Id, ct);
            if (confirmation is not null && (DateTime.UtcNow - confirmation.CreatedAt).TotalMinutes > 1)
                throw new ElwarkException(ElwarkExceptionCodes.ConfirmationAlreadySent);
            
            var code = await _confirmation.CreateResetPasswordConfirmation(account.Id, ct);

            return account.Id;
        }
    }
}