using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record ConfirmEmailSignUpCommand(AccountId Id, int Code) : IRequest;

    internal sealed class ConfirmEmailSignUpCommandHandler : IRequestHandler<ConfirmEmailSignUpCommand>
    {
        private readonly IAccountRepository _repository;

        public ConfirmEmailSignUpCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(ConfirmEmailSignUpCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var email = account.GetPrimaryEmail();
            if (email.IsConfirmed)
                return Unit.Value;

            account.ConfirmIdentity(email.Key(), DateTime.UtcNow);

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}