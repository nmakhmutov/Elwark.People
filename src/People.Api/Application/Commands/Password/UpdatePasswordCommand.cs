using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.Account;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.Password
{
    public sealed record UpdatePasswordCommand(AccountId Id, string OldPassword, string NewPassword) : IRequest;

    internal sealed class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand>
    {
        private readonly IPasswordHasher _hasher;
        private readonly IAccountRepository _repository;

        public UpdatePasswordCommandHandler(IAccountRepository repository, IPasswordHasher hasher)
        {
            _repository = repository;
            _hasher = hasher;
        }

        public async Task<Unit> Handle(UpdatePasswordCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (!account.IsPasswordEqual(request.OldPassword, _hasher.CreateHash))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordMismatch);

            var salt = _hasher.CreateSalt();
            account.SetPassword(request.NewPassword, salt, _hasher.CreateHash);

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
