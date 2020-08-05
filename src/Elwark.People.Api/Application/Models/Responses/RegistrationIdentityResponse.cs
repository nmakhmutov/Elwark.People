using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class RegistrationIdentityResponse
    {
        [DebuggerStepThrough]
        public RegistrationIdentityResponse(IdentityId identityId, Identification identification, Notification notification, bool isConfirmed)
        {
            IdentityId = identityId;
            Identification = identification;
            IsConfirmed = isConfirmed;
            Notification = notification;
        }

        public IdentityId IdentityId { get; }

        public Identification Identification { get; }
        
        public Notification Notification { get; }

        public bool IsConfirmed { get; }
    }
}