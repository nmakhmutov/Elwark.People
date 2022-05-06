using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Kafka;
using People.Worker.IntegrationEvents.Events;
using People.Worker.Services.Gravatar;
using People.Worker.Services.IpInformation;

namespace People.Worker.IntegrationEvents.EventHandling;

public sealed class AccountCreatedIntegrationEventHandler : IKafkaHandler<AccountCreatedIntegrationEvent>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IGravatarService _gravatar;
    private readonly IIpInformationService _ipInformation;

    public AccountCreatedIntegrationEventHandler(IIpInformationService ipInformation, IGravatarService gravatar,
        IIntegrationEventBus bus)
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
                ipInformation?.TimeZone,
                gravatar?.Name?.FirstOrDefault()?.FirstName,
                gravatar?.Name?.FirstOrDefault()?.LastName,
                gravatar?.ThumbnailUrl
            )
        );
    }
}
