using System;
using System.Threading.Tasks;
using People.Account.Worker.Services.Gravatar;
using People.Account.Worker.Services.IpInformation;
using People.Integration.Event;
using People.Kafka;

namespace People.Account.Worker.IntegrationEventHandlers
{
    public sealed class AccountCreatedIntegrationEventHandler : IKafkaHandler<AccountCreatedIntegrationEvent>
    {
        private readonly IIpInformationService _ipInformation;
        private readonly IGravatarService _gravatar;
        private readonly IKafkaMessageBus _bus;

        public AccountCreatedIntegrationEventHandler(IIpInformationService ipInformation, IGravatarService gravatar,
            IKafkaMessageBus bus)
        {
            _ipInformation = ipInformation;
            _gravatar = gravatar;
            _bus = bus;
        }

        public async Task HandleAsync(AccountCreatedIntegrationEvent message)
        {
            var ipInformation = await _ipInformation.GetAsync(message.Ip, message.Language);
            var gravatar = await _gravatar.GetAsync(message.Email);
            
            await _bus.PublishAsync(
                new AccountInfoReceivedIntegrationEvent(
                    Guid.NewGuid(), 
                    DateTime.UtcNow,
                    message.AccountId,
                    message.Ip,
                    ipInformation?.CountryCode,
                    ipInformation?.City,
                    ipInformation?.TimeZone,
                    gravatar?.Name?.FirstName,
                    gravatar?.Name?.LastName,
                    gravatar?.AboutMe,
                    gravatar?.ThumbnailUrl
                )
            );
        }
    }
}
