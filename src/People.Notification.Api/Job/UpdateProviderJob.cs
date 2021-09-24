using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using People.Mongo;
using People.Notification.Api.Infrastructure;
using People.Notification.Api.Infrastructure.Repositories;
using People.Notification.Api.Models;
using Polly;
using Polly.Retry;
using Quartz;

namespace People.Notification.Api.Job
{
    [DisallowConcurrentExecution]
    public class UpdateProviderJob : IJob
    {
        private readonly AsyncRetryPolicy _policy;
        private readonly NotificationDbContext _dbContext;
        private readonly IEmailProviderRepository _repository;
        private readonly ILogger<UpdateProviderJob> _logger;

        public UpdateProviderJob(IEmailProviderRepository repository, ILogger<UpdateProviderJob> logger,
            NotificationDbContext dbContext)
        {
            _repository = repository;
            _logger = logger;
            _dbContext = dbContext;
            _policy = Policy
                .Handle<MongoUpdateException>()
                .RetryForeverAsync();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var filter = Builders<EmailProvider>.Filter.And(
                Builders<EmailProvider>.Filter.Lt(x => x.UpdateAt, DateTime.UtcNow),
                Builders<EmailProvider>.Filter.Eq(x => x.IsEnabled, true)
            );

            var ids = await _dbContext.EmailProviders
                .Find(filter)
                .Project(x => x.Id)
                .ToListAsync();

            foreach (var id in ids)
            {
                await _policy.ExecuteAsync(async () =>
                {
                    var provider = await _repository.GetAsync(id);
                    if(provider is null)
                        return;
                    
                    provider.UpdateBalance();
                    await _repository.UpdateAsync(provider);
                });
                
                _logger.LogInformation("Provider '{P}' expired event sent", id);
            }
        }
    }
}
