// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class GoogleIdentity : Identity
    {
        public GoogleIdentity(string id, string name)
            : base(IdentityKey.Google(id)) =>
            Name = name;

        public string Name { get; private set; }
    }
}