using System;
using System.Threading;
using System.Threading.Tasks;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Confirmations;

public interface IConfirmationService
{
    Task<Confirmation?> GetAsync(string token, CancellationToken ct = default);

    Task<Confirmation?> GetAsync(AccountId id, CancellationToken ct = default);

    Task<Confirmation> CreateAsync(AccountId id, TimeSpan lifetime, CancellationToken ct = default);

    Task DeleteAsync(AccountId id, CancellationToken ct = default);
}
