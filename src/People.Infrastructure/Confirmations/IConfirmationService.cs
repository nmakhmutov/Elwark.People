using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using People.Domain.Aggregates.Account;

namespace People.Infrastructure.Confirmations
{
    public interface IConfirmationService
    {
        Task<Confirmation?> GetAsync(ObjectId id, CancellationToken ct = default);
        
        Task<Confirmation?> GetAsync(AccountId id, CancellationToken ct = default);
        
        Task<Confirmation> CreateAsync(AccountId id, TimeSpan lifetime, CancellationToken ct = default);
        
        Task DeleteAsync(AccountId id, CancellationToken ct = default);
    }
}