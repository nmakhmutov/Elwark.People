using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure
{
    internal sealed class NotificationContextSeed
    {
        private readonly NotificationDbContext _dbContext;

        public NotificationContextSeed(NotificationDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task SeedAsync()
        {
            var providers = await _dbContext.EmailProviders.Find(FilterDefinition<EmailProvider>.Empty).ToListAsync();
            var data = new List<EmailProvider>();

            if (providers.All(x => x.Id != EmailProvider.Type.Gmail))
                data.Add(new Gmail(100, 100));

            if (providers.All(x => x.Id != EmailProvider.Type.Sendgrid))
                data.Add(new Sendgrid(100, 100));

            if (data.Count > 0)
                await _dbContext.EmailProviders.InsertManyAsync(data, new InsertManyOptions());
        }
    }
}
