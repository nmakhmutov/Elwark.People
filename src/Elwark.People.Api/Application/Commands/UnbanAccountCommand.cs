using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class UnbanAccountCommand : IRequest
    {
        [DebuggerStepThrough]
        public UnbanAccountCommand(AccountId id) =>
            Id = id;

        public AccountId Id { get; }
    }

    public class UnbanAccountCommandHandler : IRequestHandler<UnbanAccountCommand>
    {
        private readonly IAccountRepository _repository;

        public UnbanAccountCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(UnbanAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.Id, cancellationToken);
            if (account is null)
                return Unit.Value;

            account.RemoveBan();

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}