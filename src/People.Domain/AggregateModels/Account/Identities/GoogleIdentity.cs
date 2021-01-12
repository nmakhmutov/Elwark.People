using System;

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class GoogleIdentity : Identity
    {
        public GoogleIdentity(string value, DateTime? confirmedAt)
            : base(new IdentityKey(IdentityType.Google, value), confirmedAt)
        {
        }
    }
}