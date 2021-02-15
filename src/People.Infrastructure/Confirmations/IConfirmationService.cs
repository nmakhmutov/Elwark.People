using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Confirmations
{
    public interface IConfirmationService
    {
        Task<Confirmation?> GetAsync(AccountId id, CancellationToken ct = default);
        
        Task<uint> CreateAsync(AccountId id, TimeSpan lifetime, CancellationToken ct = default);
        
        Task DeleteAsync(ObjectId id, CancellationToken ct = default);
    }
}