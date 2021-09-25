namespace People.Account.Domain.Seed
{
    public interface IAggregateRoot
    {
        public int Version { get; set; }
    }
}
