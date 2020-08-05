using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Abstractions;
using Elwark.People.Shared;
using Elwark.People.Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.Background
{
    public class AccountBansExpiredHostedService : IntervalBackgroundService<AccountBansExpiredHostedService>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IDatabaseQueryExecutor _executor;

        public AccountBansExpiredHostedService(ILogger<AccountBansExpiredHostedService> logger,
            IIntegrationEventPublisher eventPublisher, IDatabaseQueryExecutor executor)
            : base(logger)
        {
            _eventPublisher = eventPublisher;
            _executor = executor;
        }

        protected override TimeSpan Delay => TimeSpan.FromHours(1);

        protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
        {
            var ids = await _executor.MultiplyAsync(@"
SELECT account_id 
FROM bans 
WHERE expired_at < now() at TIME ZONE 'utc';",
                new Dictionary<string, object>(),
                reader => new AccountId(reader.GetFieldValue<long>(0)),
                cancellationToken
            );

            foreach (var id in ids)
            {
                await _eventPublisher.PublishAsync(new AccountBanExpiredIntegrationEvent(id), cancellationToken);
                Logger.LogInformation("Ban for account {id} is expired", id);
            }
        }
    }
}