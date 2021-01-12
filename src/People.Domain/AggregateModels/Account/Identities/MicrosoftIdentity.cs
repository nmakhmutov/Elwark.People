using System;

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class MicrosoftIdentity : Identity
    {
        public MicrosoftIdentity(string value, DateTime? confirmedAt) 
            : base(new IdentityKey(IdentityType.Microsoft, value), confirmedAt)
        {
        }
    }
}