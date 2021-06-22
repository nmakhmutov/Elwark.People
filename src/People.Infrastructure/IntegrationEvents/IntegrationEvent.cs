namespace People.Infrastructure.IntegrationEvents
{
    public static class IntegrationEvent
    {
        public const string CreatedAccounts = "people.account.created";
        
        public const string UpdatedAccounts = "people.account.updated";

        public const string CollectedInformation = "people.account.collected";
        
        public const string EmailMessages = "people.notification.email";
        
        public const string ExpiredProviders = "people.notification.provider.expired";
    }
}
