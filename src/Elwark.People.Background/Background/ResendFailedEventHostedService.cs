using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Shared;
using Elwark.People.Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.Background
{
    public class ResendFailedEventHostedService : IntervalBackgroundService<ResendFailedEventHostedService>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IDatabaseQueryExecutor _executor;

        public ResendFailedEventHostedService(ILogger<ResendFailedEventHostedService> logger,
            IIntegrationEventPublisher eventPublisher, IDatabaseQueryExecutor executor)
            : base(logger)
        {
            _eventPublisher = eventPublisher;
            _executor = executor;
        }

        protected override TimeSpan Delay => TimeSpan.FromMinutes(1);

        protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
        {
            var ids = await _executor.MultiplyAsync(@"
SELECT id 
FROM integration_event_logs 
WHERE state <> 2 AND creation_time < (now() - INTERVAL '1 minute')",
                new Dictionary<string, object>(),
                reader => reader.GetFieldValue<Guid>(0),
                cancellationToken);

            foreach (var id in ids)
            {
                await _eventPublisher.PublishAsync(new IntegrationEventFailedIntegrationEvent(id), cancellationToken);
                Logger.LogInformation("Failed integration event {id} resent", id);
            }
        }
    }
}