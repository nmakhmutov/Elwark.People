using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;

namespace Elwark.People.Infrastructure.Confirmation
{
    public interface IConfirmationStore
    {
        Task<ConfirmationModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ConfirmationModel> CreateAsync(ConfirmationModel confirmation,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ConfirmationModel> GetAsync(IdentityId id, long code, CancellationToken cancellationToken = default);
    }
}