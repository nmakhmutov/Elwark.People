// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System;

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class FacebookIdentityModel : IdentityModel
    {
        public FacebookIdentityModel(string id, string name, DateTime? confirmedAt = null)
            : base(new FacebookIdentity(id), confirmedAt) =>
            Name = name;

        public string Name { get; private set; }

        public override FacebookIdentity GetIdentity() =>
            new(Value);
    }
}