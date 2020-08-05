namespace Elwark.People.Domain.SeedWork
{
    public interface IRepository : IAggregateRoot
    {
        IUnitOfWork UnitOfWork { get; }
    }
}