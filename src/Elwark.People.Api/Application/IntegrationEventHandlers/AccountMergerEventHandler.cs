using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;

namespace Elwark.People.Api.Application.IntegrationEventHandlers
{
    public class AccountMergerIntegrationEventHandler : IIntegrationEventHandler<MergeAccountInformationIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public AccountMergerIntegrationEventHandler(IMediator mediator) =>
            _mediator = mediator;

        public async Task HandleAsync(MergeAccountInformationIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var command = new MergeAccountInformationCommand(
                evt.AccountId,
                evt.FirstName,
                evt.LastName,
                evt.Picture,
                evt.Timezone,
                evt.CountryCode,
                evt.City,
                evt.Bio,
                evt.Gender,
                evt.Birthday
            );

            await _mediator.Send(command, cancellationToken);
        }
    }
}