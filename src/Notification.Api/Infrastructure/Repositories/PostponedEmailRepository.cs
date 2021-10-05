using MongoDB.Bson;
using MongoDB.Driver;
using Notification.Api.Models;
using Common.Mongo;

namespace Notification.Api.Infrastructure.Repositories;

internal sealed class PostponedEmailRepository : IPostponedEmailRepository
{
    private readonly NotificationDbContext _dbContext;

    public PostponedEmailRepository(NotificationDbContext dbContext) =>
        _dbContext = dbContext;

    public Task<PostponedEmail> GetAsync(ObjectId id, CancellationToken ct) =>
        _dbContext.PostponedEmails
            .Find(Builders<PostponedEmail>.Filter.Eq(x => x.Id, id))
            .FirstOrDefaultAsync(ct);

    public async Task<PostponedEmail> CreateAsync(PostponedEmail entity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        entity.Version++;
        await _dbContext.PostponedEmails.InsertOneAsync(entity, new InsertOneOptions(), ct);

        return entity;
    }

    public async Task UpdateAsync(PostponedEmail entity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var filter = Builders<PostponedEmail>.Filter.Eq(x => x.Id, entity.Id);

        entity.Version = (entity.Version == int.MaxValue ? int.MinValue : entity.Version) + 1;

        var result = await _dbContext.PostponedEmails.ReplaceOneAsync(filter, entity, new ReplaceOptions(), ct);
        if (result.ModifiedCount == 0)
            throw new MongoUpdateException($"Entity with id '{entity.Id}' not updated");
    }

    public Task DeleteAsync(ObjectId id, CancellationToken ct)
    {
        var filter = Builders<PostponedEmail>.Filter.Eq(x => x.Id, id);

        return _dbContext.PostponedEmails.DeleteOneAsync(filter, new DeleteOptions(), ct);
    }
}
