using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;
using Newtonsoft.Json;

namespace Elwark.People.Api.Infrastructure.Services.Confirmation
{
    [DataContract]
    public class ConfirmationData
    {
        [DebuggerStepThrough]
        public ConfirmationData(Guid confirmationId, IdentityId identityId, ConfirmationType type, long code)
        {
            ConfirmationId = confirmationId;
            IdentityId = identityId;
            Type = type;
            Code = code;
        }

        [DataMember, JsonProperty("a")]
        public IdentityId IdentityId { get; }

        [DataMember, JsonProperty("b")]
        public Guid ConfirmationId { get; }

        [DataMember, JsonProperty("c")]
        public ConfirmationType Type { get; }

        [DataMember, JsonProperty("d")]
        public long Code { get; }
    }
}