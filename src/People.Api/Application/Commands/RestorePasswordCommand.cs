using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure.Password;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record RestorePasswordCommand(AccountId Id, int Code, string Password) : IRequest;
    
    public sealed class RestorePasswordCommandHandler : IRequestHandler<RestorePasswordCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IPasswordHasher _hasher;

        public RestorePasswordCommandHandler(IAccountRepository repository, IPasswordHasher hasher)
        {
            _repository = repository;
            _hasher = hasher;
        }

        public async Task<Unit> Handle(RestorePasswordCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var salt = _hasher.CreateSalt();
            account.SetPassword(request.Password, salt, _hasher.CreateHash);

            await _repository.UpdateAsync(account, ct);
            
            return Unit.Value;
        }
    }
}