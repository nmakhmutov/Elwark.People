using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record ChangeEmailTypeCommand(AccountId Id, string Email, EmailType Type) : IRequest;

    internal sealed class ChangeEmailTypeCommandHandler : IRequestHandler<ChangeEmailTypeCommand>
    {
        private readonly IAccountRepository _repository;

        public ChangeEmailTypeCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(ChangeEmailTypeCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.ChangeEmailType(new EmailIdentity(request.Email), request.Type);

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
