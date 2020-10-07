using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Infrastructure.Confirmation
{
    public interface IConfirmationStore
    {
        Task<bool> CreateAsync(ConfirmationModel confirmation, TimeSpan expiry);

        Task<ConfirmationModel?> GetAsync(IdentityId id, ConfirmationType type);

        Task DeleteAsync(IdentityId id, ConfirmationType type);
    }
}