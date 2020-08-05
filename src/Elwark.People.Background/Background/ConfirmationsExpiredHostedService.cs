using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Shared;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.Background
{
    public class ConfirmationsExpiredHostedService : IntervalBackgroundService<ConfirmationsExpiredHostedService>
    {
        private readonly IDatabaseQueryExecutor _executor;

        public ConfirmationsExpiredHostedService(ILogger<ConfirmationsExpiredHostedService> logger,
            IDatabaseQueryExecutor executor)
            : base(logger) => _executor = executor;

        protected override TimeSpan Delay =>
            TimeSpan.FromMinutes(10);

        protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
        {
            await _executor.ExecuteAsync(@"
DELETE
FROM confirmations 
WHERE expired_at < now() at TIME ZONE 'utc';",
                new Dictionary<string, object>(),
                cancellationToken);
            
            Logger.LogInformation("Outdated confirmations has been removed");
        }
    }
}