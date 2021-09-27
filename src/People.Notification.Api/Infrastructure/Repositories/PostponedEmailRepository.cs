using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using People.Mongo;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure.Repositories
{
    internal sealed class PostponedEmailRepository : IPostponedEmailRepository
    {
        private readonly NotificationDbContext _dbContext;

        public PostponedEmailRepository(NotificationDbContext dbContext) =>
            _dbContext = dbContext;

        public async IAsyncEnumerable<PostponedEmail> GetAsync(DateTime sendAt,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var filter = Builders<PostponedEmail>.Filter.Lt(x => x.SendAt, sendAt);
            using var cursor = await _dbContext.DelayedEmails
                .FindAsync(
                    filter,
                    new FindOptions<PostponedEmail>
                    {
                        BatchSize = 100,
                        Sort = Builders<PostponedEmail>.Sort.Descending(x => x.SendAt)
                    },
                    ct
                );

            while (await cursor.MoveNextAsync(ct))
                foreach (var item in cursor.Current)
                    yield return item;
        }

        public async Task<PostponedEmail> CreateAsync(PostponedEmail entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await _dbContext.DelayedEmails.InsertOneAsync(entity, new InsertOneOptions(), ct);

            return entity;
        }

        public async Task UpdateAsync(PostponedEmail entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var filter = Builders<PostponedEmail>.Filter.Eq(x => x.Id, entity.Id);

            var result = await _dbContext.DelayedEmails.ReplaceOneAsync(filter, entity, new ReplaceOptions(), ct);
            if (result.ModifiedCount == 0)
                throw new MongoUpdateException($"Entity with id '{entity.Id}' not updated");
        }

        public Task DeleteAsync(ObjectId id, CancellationToken ct)
        {
            var filter = Builders<PostponedEmail>.Filter.Eq(x => x.Id, id);

            return _dbContext.DelayedEmails.DeleteOneAsync(filter, new DeleteOptions(), ct);
        }
    }
}
