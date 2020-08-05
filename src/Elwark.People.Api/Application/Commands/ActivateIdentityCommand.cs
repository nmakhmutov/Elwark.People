using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class ActivateIdentityCommand : IRequest
    {
        public ActivateIdentityCommand(IdentityId id) =>
            Id = id;

        public IdentityId Id { get; }
    }

    public class ActivateIdentityCommandHandler : IRequestHandler<ActivateIdentityCommand>
    {
        private readonly IAccountRepository _repository;

        public ActivateIdentityCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(ActivateIdentityCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.Id, cancellationToken)
                          ?? throw ElwarkIdentificationException.NotFound();

            account.ConfirmIdentity(request.Id);

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}