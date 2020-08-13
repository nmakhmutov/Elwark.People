using System;
using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models
{
    public class IdentityModel
    {
        [DebuggerStepThrough]
        public IdentityModel(IdentityId identityId, AccountId accountId, Identification identification, Notification notification,
            DateTimeOffset? confirmedAt, DateTimeOffset createdAt)
        {
            IdentityId = identityId;
            AccountId = accountId;
            Identification = identification;
            Notification = notification;
            ConfirmedAt = confirmedAt;
            CreatedAt = createdAt;
        }

        public IdentityId IdentityId { get; }
        
        public AccountId AccountId { get; }
        
        public Identification Identification { get; }

        public Notification Notification { get; }

        public DateTimeOffset? ConfirmedAt { get; }

        public DateTimeOffset CreatedAt { get; }
    }
}