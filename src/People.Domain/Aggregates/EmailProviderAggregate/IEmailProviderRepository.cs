using System.Threading;
using System.Threading.Tasks;
using People.Domain.SeedWork;

namespace People.Domain.Aggregates.EmailProviderAggregate
{
    public interface IEmailProviderRepository : IRepository<EmailProvider.Type, EmailProvider>
    {
        Task<EmailProvider?> GetNextAsync(CancellationToken ct = default);
    }
}
