using People.Domain.SeedWork;

namespace People.Domain.AggregatesModel.AccountAggregate;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetAsync(long id, CancellationToken ct = default);
}
