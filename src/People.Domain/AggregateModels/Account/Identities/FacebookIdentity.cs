// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class FacebookIdentity : Identity
    {
        public FacebookIdentity(string id, string name)
            : base(new IdentityKey(IdentityType.Facebook, id)) =>
            Name = name;

        public string Name { get; private set; }
    }
}