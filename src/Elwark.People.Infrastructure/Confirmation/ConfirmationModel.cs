using System;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Infrastructure.Confirmation
{
    public class ConfirmationModel
    {
        public ConfirmationModel(IdentityId identityId, ConfirmationType type, long code, DateTimeOffset expiredAt)
        {
            IdentityId = identityId;
            Type = type;
            Code = code;
            ExpiredAt = expiredAt;
        }

        public Guid Id { get; private set; } = Guid.Empty;

        public IdentityId IdentityId { get; }

        public ConfirmationType Type { get; }

        public long Code { get; }

        public DateTimeOffset ExpiredAt { get; }
    }
}