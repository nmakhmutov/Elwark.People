using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;

namespace Elwark.People.Api.Application.IntegrationEventHandlers
{
    public class BanExpiredIntegrationEventHandler : IIntegrationEventHandler<AccountBanExpiredIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public BanExpiredIntegrationEventHandler(IMediator mediator) =>
            _mediator = mediator;

        public Task HandleAsync(AccountBanExpiredIntegrationEvent evt, CancellationToken cancellationToken) =>
            _mediator.Send(new UnbanAccountCommand(evt.AccountId), cancellationToken);
    }
}