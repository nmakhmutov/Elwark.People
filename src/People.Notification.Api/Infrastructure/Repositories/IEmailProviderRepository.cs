using System.Threading;
using System.Threading.Tasks;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure.Repositories
{
    public interface IEmailProviderRepository
    {
        Task<EmailProvider?> GetNextAsync(CancellationToken ct = default);
        
        Task<EmailProvider?> GetAsync(EmailProvider.Type key, CancellationToken ct = default);
        
        Task<EmailProvider> CreateAsync(EmailProvider entity, CancellationToken ct = default);
        
        Task UpdateAsync(EmailProvider entity, CancellationToken ct = default);
        
        Task DeleteAsync(EmailProvider.Type key, CancellationToken ct = default);
    }
}
