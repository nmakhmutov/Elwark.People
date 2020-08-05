using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class DeleteIdentityCommand : IRequest
    {
        public DeleteIdentityCommand(AccountId accountId, IdentityId identityId)
        {
            AccountId = accountId;
            IdentityId = identityId;
        }

        public AccountId AccountId { get; }

        public IdentityId IdentityId { get; }
    }

    public class DeleteIdentityCommandHandler : IRequestHandler<DeleteIdentityCommand>
    {
        private readonly IAccountRepository _repository;

        public DeleteIdentityCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(DeleteIdentityCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            account.RemoveIdentity(request.IdentityId);

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}