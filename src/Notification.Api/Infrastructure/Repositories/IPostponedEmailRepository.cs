using MongoDB.Bson;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure.Repositories;

public interface IPostponedEmailRepository
{
    Task<PostponedEmail> GetAsync(ObjectId id, CancellationToken ct = default);

    Task<PostponedEmail> CreateAsync(PostponedEmail entity, CancellationToken ct = default);

    Task UpdateAsync(PostponedEmail entity, CancellationToken ct = default);

    Task DeleteAsync(ObjectId id, CancellationToken ct = default);
}
