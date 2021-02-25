namespace People.Infrastructure.IntegrationEvents
{
    public static class IntegrationEvent
    {
        public const string CreatedAccounts = "people.created_accounts";

        public const string CollectedInformation = "people.collected_information";
        
        public const string EmailMessages = "people.email_messages";
        
        public const string ExpiredProviders = "people.expired_providers";
    }
}