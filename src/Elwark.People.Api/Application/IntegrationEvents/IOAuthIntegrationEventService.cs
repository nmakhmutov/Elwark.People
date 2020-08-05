using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;

namespace Elwark.People.Api.Application.IntegrationEvents
{
    public interface IOAuthIntegrationEventService
    {
        Task PublishThroughEventBusAsync(IntegrationEvent evt, CancellationToken cancellationToken = default);
    }
}