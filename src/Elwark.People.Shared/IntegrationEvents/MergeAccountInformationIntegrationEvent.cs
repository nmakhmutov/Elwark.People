using System;
using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class MergeAccountInformationIntegrationEvent : IntegrationEvent
    {
        public AccountId AccountId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public Uri? Picture { get; set; }

        public string? Timezone { get; set; }

        public string? CountryCode { get; set; }

        public string? City { get; set; }
        
        public string? Bio { get; set; }
        
        public Gender? Gender { get; set; }
        
        public DateTime? Birthday { get; set; }
    }
}