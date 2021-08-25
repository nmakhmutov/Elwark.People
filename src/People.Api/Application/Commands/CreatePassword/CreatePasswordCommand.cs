using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Api.Infrastructure.Password;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.CreatePassword
{
    public sealed record CreatePasswordCommand(
        AccountId Id,
        ObjectId ConfirmationId,
        uint ConfirmationCode,
        string Password
    ) : IRequest;

    public sealed class CreatePasswordCommandHandler : IRequestHandler<CreatePasswordCommand>
    {
        private readonly IConfirmationService _confirmation;
        private readonly IPasswordHasher _hasher;
        private readonly IAccountRepository _repository;

        public CreatePasswordCommandHandler(IAccountRepository repository, IPasswordHasher hasher,
            IConfirmationService confirmation)
        {
            _repository = repository;
            _hasher = hasher;
            _confirmation = confirmation;
        }

        public async Task<Unit> Handle(CreatePasswordCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);
            
            account.SetPassword(request.Password, _hasher);

            await _repository.UpdateAsync(account, ct);
            await _confirmation.DeleteAsync(account.Id, ct);

            return Unit.Value;
        }
    }
}
