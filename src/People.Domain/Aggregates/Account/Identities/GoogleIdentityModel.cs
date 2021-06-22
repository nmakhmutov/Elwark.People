// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System;

namespace People.Domain.Aggregates.Account.Identities
{
    public class GoogleIdentityModel : IdentityModel
    {
        public GoogleIdentityModel(string id, string name, DateTime? confirmedAt = null)
            : base(new GoogleIdentity(id),confirmedAt) =>
            Name = name;

        public string Name { get; private set; }

        public override GoogleIdentity GetIdentity() =>
            new(Value);
    }
}