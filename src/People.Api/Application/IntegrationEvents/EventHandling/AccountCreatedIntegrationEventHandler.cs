using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountCreatedIntegrationEventHandler : IIntegrationEventHandler<AccountCreatedIntegrationEvent>
{
    private readonly IConfirmationService _confirmation;
    private readonly IGravatarService _gravatar;
    private readonly IEnumerable<IIpService> _ipServices;
    private readonly ILogger<AccountCreatedIntegrationEventHandler> _logger;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AccountCreatedIntegrationEventHandler(
        IConfirmationService confirmation,
        IGravatarService gravatar,
        IEnumerable<IIpService> ipServices,
        IAccountRepository repository,
        TimeProvider timeProvider,
        ILogger<AccountCreatedIntegrationEventHandler> logger
    )
    {
        _confirmation = confirmation;
        _gravatar = gravatar;
        _repository = repository;
        _ipServices = ipServices;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(AccountCreatedIntegrationEvent message, CancellationToken ct)
    {
        var account = await _repository.GetAsync(message.AccountId, ct);

        if (account is null)
            return;

        var ipInformation = await GetIpInformation(message.Ip, account.Language);

        if (ipInformation is not null)
        {
            _ = RegionCode.TryParse(ipInformation.Region, out var region);
            _ = CountryCode.TryParse(ipInformation.CountryCode, out var country);
            _ = TimeZone.TryParse(ipInformation.TimeZone, out var timeZone);

            account.Update(account.Language, region, country, timeZone, _timeProvider);
        }

        var gravatar = await _gravatar.GetAsync(account.GetPrimaryEmail());

        if (gravatar is not null)
        {
            var firstName = gravatar.Name?.FirstOrDefault()?.FirstName ?? account.Name.FirstName;
            var lastName = gravatar.Name?.FirstOrDefault()?.LastName ?? account.Name.LastName;
            account.Update(firstName, lastName, _timeProvider);

            if (Uri.TryCreate(gravatar.ThumbnailUrl, UriKind.Absolute, out var image))
                account.Update(image, _timeProvider);
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        await _confirmation.DeleteAsync(message.AccountId, ct);
    }

    private async Task<IpInformation?> GetIpInformation(string ip, Language language)
    {
        foreach (var ipService in _ipServices)
        {
            try
            {
                var result = await ipService.GetAsync(ip, language.ToString());

                if (result is not null)
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IP geolocation provider failed for {Ip}", ip);
            }
        }

        return null;
    }
}
