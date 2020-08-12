using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.EventBus.Logging.EF;
using Elwark.People.Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.IntegrationEventHandlers
{
    public class RetryEventHandler : IIntegrationEventHandler<IntegrationEventFailedIntegrationEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IIntegrationEventLogService _integrationEventLogService;
        private readonly ILogger<RetryEventHandler> _logger;

        public RetryEventHandler(IIntegrationEventLogService integrationEventLogService,
            IIntegrationEventPublisher eventPublisher, ILogger<RetryEventHandler> logger)
        {
            _integrationEventLogService = integrationEventLogService ??
                                          throw new ArgumentNullException(nameof(integrationEventLogService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(IntegrationEventFailedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var entity = await _integrationEventLogService.GetAsync(evt.EventLogId, cancellationToken);
            if (entity is null || entity.State == EventStateEnum.Published)
                return;

            try
            {
                await _integrationEventLogService.MarkEventAsInProgressAsync(entity.EventId, cancellationToken);
                var type = Type.GetType(entity.EventTypeName)
                           ?? throw new ArgumentNullException(nameof(entity.EventTypeName),
                               @"Unknown integration event type");

                if (JsonConvert.DeserializeObject(entity.Content, type) is IntegrationEvent failedEvent)
                    await _eventPublisher.PublishAsync(failedEvent, cancellationToken);

                await _integrationEventLogService.MarkEventAsPublishedAsync(entity.EventId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR publishing integration event: {IntegrationEventId} from {AppName}",
                    entity.EventId, Program.AppName);

                await _integrationEventLogService.MarkEventAsFailedAsync(entity.EventId, cancellationToken);
            }
        }
    }
}