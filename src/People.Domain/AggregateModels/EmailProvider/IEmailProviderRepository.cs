using System.Threading;
using System.Threading.Tasks;
using People.Domain.SeedWork;

namespace People.Domain.AggregateModels.EmailProvider
{
    public interface IEmailProviderRepository : IRepository<EmailProviderType, EmailProvider>
    {
        Task<EmailProvider?> GetNextAsync(CancellationToken ct = default);
    }
}