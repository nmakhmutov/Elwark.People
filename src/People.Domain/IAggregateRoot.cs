namespace People.Domain
{
    public interface IAggregateRoot
    {
        public int Version { get; set; }
    }
}
