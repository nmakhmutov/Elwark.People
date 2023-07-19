namespace People.Domain.SeedWork;

public interface IAggregateRoot
{
    public void SetAsUpdated(TimeProvider provider);
}
