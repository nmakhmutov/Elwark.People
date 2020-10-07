using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Infrastructure.Confirmation
{
    [DataContract]
    public sealed class ConfirmationModel
    {
        [DebuggerStepThrough]
        public ConfirmationModel(IdentityId identityId, ConfirmationType type, long code)
        {
            IdentityId = identityId;
            Type = type;
            Code = code;
            CreatedAt = DateTime.UtcNow;
        }

        [DataMember(Name = "a", IsRequired = true)]
        public IdentityId IdentityId { get; }

        [DataMember(Name = "b", IsRequired = true)]
        public ConfirmationType Type { get; }

        [DataMember(Name = "c", IsRequired = true)]
        public long Code { get; }
        
        [DataMember(Name = "d", IsRequired = true)]
        public DateTime CreatedAt { get; }
    }
}