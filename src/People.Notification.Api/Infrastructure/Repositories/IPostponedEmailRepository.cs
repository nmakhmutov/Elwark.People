using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure.Repositories
{
    public interface IPostponedEmailRepository
    {
        Task<PostponedEmail> GetAsync(ObjectId id, CancellationToken ct = default);

        Task<PostponedEmail> CreateAsync(PostponedEmail entity, CancellationToken ct = default);

        Task UpdateAsync(PostponedEmail entity, CancellationToken ct = default);

        Task DeleteAsync(ObjectId id, CancellationToken ct = default);
    }
}
