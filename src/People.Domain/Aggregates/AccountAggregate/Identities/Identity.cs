namespace People.Domain.Aggregates.AccountAggregate.Identities
{
    public abstract record Identity(Connection.Type Type, string Value)
    {
        public record Email(string Value) : Identity(Connection.Type.Email, Value.ToLowerInvariant());

        public record Google(string Value) : Identity(Connection.Type.Google, Value);

        public record Microsoft(string Value) : Identity(Connection.Type.Microsoft, Value);
    }
}
