using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Shared;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.Background
{
    public class IntegrationEventLogsExpiredHostedService :
        IntervalBackgroundService<IntegrationEventLogsExpiredHostedService>
    {
        private readonly IDatabaseQueryExecutor _executor;

        public IntegrationEventLogsExpiredHostedService(ILogger<IntegrationEventLogsExpiredHostedService> logger,
            IDatabaseQueryExecutor executor)
            : base(logger) => _executor = executor;

        protected override TimeSpan Delay => TimeSpan.FromHours(12);

        protected override Task ExecuteTaskAsync(CancellationToken cancellationToken) =>
            _executor.ExecuteAsync(@"
DELETE FROM integration_event_logs 
WHERE creation_time < now() at TIME ZONE 'utc' - interval '2 day'",
                new Dictionary<string, object>(),
                cancellationToken);
    }
}