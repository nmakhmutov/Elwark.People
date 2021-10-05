using Notification.Api.Models;

namespace Notification.Api.Infrastructure.Repositories;

public interface IEmailProviderRepository
{
    Task<EmailProvider?> GetNextAsync(CancellationToken ct = default);

    Task<EmailProvider?> GetAsync(EmailProvider.Type key, CancellationToken ct = default);

    Task<EmailProvider> CreateAsync(EmailProvider entity, CancellationToken ct = default);

    Task UpdateAsync(EmailProvider entity, CancellationToken ct = default);

    Task DeleteAsync(EmailProvider.Type key, CancellationToken ct = default);
}
