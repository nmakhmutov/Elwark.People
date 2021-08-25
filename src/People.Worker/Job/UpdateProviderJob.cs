using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using People.Domain.Aggregates.EmailProviderAggregate;
using People.Infrastructure;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using Quartz;

namespace People.Worker.Job
{
    [DisallowConcurrentExecution]
    public class UpdateProviderJob : IJob
    {
        private readonly PeopleDbContext _dbContext;
        private readonly IKafkaMessageBus _bus;
        private readonly ILogger<UpdateProviderJob> _logger;

        public UpdateProviderJob(PeopleDbContext dbContext, IKafkaMessageBus bus, ILogger<UpdateProviderJob> logger)
        {
            _dbContext = dbContext;
            _bus = bus;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var filter = Builders<EmailProvider>.Filter.And(
                Builders<EmailProvider>.Filter.Lt(x => x.UpdateAt, DateTime.UtcNow),
                Builders<EmailProvider>.Filter.Eq(x => x.IsEnabled, true)
            );

            var cursor = await _dbContext.EmailProviders.FindAsync(filter);
            var providers = await cursor.ToListAsync();

            foreach (var provider in providers)
            {
                var evt = new ProviderExpiredIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, provider.Id);
                await _bus.PublishAsync(evt);
                
                _logger.LogInformation("Provider '{P}' expired event sent", provider.Id);
            }
        }
    }
}
