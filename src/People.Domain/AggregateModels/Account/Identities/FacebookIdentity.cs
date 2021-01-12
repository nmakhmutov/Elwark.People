using System;

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class FacebookIdentity : Identity
    {
        public FacebookIdentity(string value, DateTime? confirmedAt)
            : base(new IdentityKey(IdentityType.Facebook, value), confirmedAt)
        {
        }
    }
}