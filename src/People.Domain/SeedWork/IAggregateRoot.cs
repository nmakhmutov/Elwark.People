namespace People.Domain.SeedWork;

public interface IAggregateRoot
{
    void SetAsUpdated(TimeProvider provider);
}
