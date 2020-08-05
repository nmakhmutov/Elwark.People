using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.EventBus.Logging.EF;
using Microsoft.Extensions.DependencyInjection;

namespace Elwark.People.Api.Application.IntegrationEvents
{
    public class OAuthIntegrationEventService : IOAuthIntegrationEventService
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IServiceProvider _services;

        public OAuthIntegrationEventService(IServiceProvider services, IIntegrationEventPublisher eventPublisher)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _eventPublisher = eventPublisher;
        }

        public async Task PublishThroughEventBusAsync(IntegrationEvent evt, CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var logService = new IntegrationEventLogService(context);

            await ResilientTransaction.New(context)
                .ExecuteAsync(() => logService.SaveEventAsync(evt, cancellationToken));

            try
            {
                await _eventPublisher.PublishAsync(evt, cancellationToken);
                await logService.MarkEventAsPublishedAsync(evt.Id, cancellationToken);
            }
            catch (Exception)
            {
                await logService.MarkEventAsFailedAsync(evt.Id, cancellationToken);
            }
        }
    }
}