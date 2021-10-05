namespace People.Domain.Seed;

public interface IAggregateRoot
{
    public int Version { get; set; }
}
