using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Domain.Entities;
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
    private readonly IAccountRepository _repository;

    public AccountCreatedIntegrationEventHandler(
        IConfirmationService confirmation,
        IGravatarService gravatar,
        IEnumerable<IIpService> ipServices,
        IAccountRepository repository
    )
    {
        _confirmation = confirmation;
        _gravatar = gravatar;
        _repository = repository;
        _ipServices = ipServices;
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

            account.Update(account.Language, region, country, timeZone);
        }

        var gravatar = await _gravatar.GetAsync(account.GetPrimaryEmail());

        if (gravatar is not null)
        {
            var firstName = gravatar.Name?.FirstOrDefault()?.FirstName ?? account.Name.FirstName;
            var lastName = gravatar.Name?.FirstOrDefault()?.LastName ?? account.Name.LastName;
            account.Update(firstName, lastName);

            if (Uri.TryCreate(gravatar.ThumbnailUrl, UriKind.Absolute, out var image))
                account.Update(image);
        }

        _repository.Update(account);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        await _confirmation.DeleteAsync(message.AccountId, ct);
    }

    private async Task<IpInformation?> GetIpInformation(string ip, Language language)
    {
        foreach (var ipService in _ipServices)
        {
            var result = await ipService.GetAsync(ip, language.ToString());

            if (result is not null)
                return result;
        }

        return null;
    }
}
