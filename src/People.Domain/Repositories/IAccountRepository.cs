using People.Domain.Entities;
using People.Domain.SeedWork;

namespace People.Domain.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetAsync(long id, CancellationToken ct = default);
}
