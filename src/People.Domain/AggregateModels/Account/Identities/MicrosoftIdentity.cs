// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class MicrosoftIdentity : Identity
    {
        public MicrosoftIdentity(string id, string name)
            : base(IdentityKey.Microsoft(id)) =>
            Name = name;

        public string Name { get; private set; }
    }
}