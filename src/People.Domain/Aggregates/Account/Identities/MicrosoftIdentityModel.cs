// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System;

namespace People.Domain.Aggregates.Account.Identities
{
    public sealed class MicrosoftIdentityModel : IdentityModel
    {
        public MicrosoftIdentityModel(string id, string name, DateTime? confirmedAt = null)
            : base(new MicrosoftIdentity(id), confirmedAt) =>
            Name = name;

        public string Name { get; private set; }

        public override MicrosoftIdentity GetIdentity() =>
            new(Value);
    }
}