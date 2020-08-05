using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class ChangeNotificationTypeCommand : IRequest
    {
        public ChangeNotificationTypeCommand(IdentityId identityId, NotificationType type)
        {
            Type = type;
            IdentityId = identityId;
        }

        public IdentityId IdentityId { get; }

        public NotificationType Type { get; }
    }

    public class ChangeNotificationTypeCommandHandler : IRequestHandler<ChangeNotificationTypeCommand>
    {
        private readonly IAccountRepository _repository;

        public ChangeNotificationTypeCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(ChangeNotificationTypeCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.IdentityId, cancellationToken)
                          ?? throw ElwarkIdentificationException.NotFound();

            account.SetNotificationType(request.IdentityId, request.Type);

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}