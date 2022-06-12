namespace People.Domain.SeedWork;

public interface ITimeProvider
{
    public DateTime Now { get; }
}
