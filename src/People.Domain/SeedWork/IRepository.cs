namespace People.Domain.SeedWork;

public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }

    Task<T> AddAsync(T entity, CancellationToken ct = default);
    
    void Update(T account);
}
