using System;
using System.Net;
using System.Threading.Tasks;
using Common.Kafka;
using People.Domain.Aggregates.AccountAggregate;
using People.Infrastructure.Countries;
using AccountInfoReceivedIntegrationEvent = People.Api.Application.IntegrationEvents.Events.AccountInfoReceivedIntegrationEvent;
using TimeZone = People.Domain.Aggregates.AccountAggregate.TimeZone;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountInfoEventHandler : IKafkaHandler<AccountInfoReceivedIntegrationEvent>
{
    private readonly ICountryService _country;
    private readonly IIpAddressHasher _ipHasher;
    private readonly IAccountRepository _repository;

    public AccountInfoEventHandler(IAccountRepository repository, ICountryService country, IIpAddressHasher ipHasher)
    {
        _repository = repository;
        _country = country;
        _ipHasher = ipHasher;
    }

    public async Task HandleAsync(AccountInfoReceivedIntegrationEvent message)
    {
        var account = await _repository.GetAsync(message.AccountId);
        if (account is null)
            return;

        var countryCode = await GetCountryCodeAsync(message.CountryCode);
        var timezone = TimeZone.TryParse(message.TimeZone, out var timeZone) ? timeZone : account.TimeZone;
        var picture = account.Picture == Account.DefaultPicture && message.Image is not null
            ? new Uri(message.Image)
            : account.Picture;
        var name = account.Name with
        {
            FirstName = account.Name.FirstName ?? message.FirstName?[..Name.FirstNameLength],
            LastName = account.Name.LastName ?? message.LastName?[..Name.LastNameLength]
        };

        account.Update(name, countryCode, timezone, picture);
        account.SetRegistration(IPAddress.Parse(message.Ip), countryCode, _ipHasher);

        await _repository.UpdateAsync(account);
    }

    private async Task<CountryCode> GetCountryCodeAsync(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return CountryCode.Empty;

        var country = await _country.GetAsync(code);
        return CountryCode.TryParse(country?.Alpha2Code, out var countryCode) ? countryCode : CountryCode.Empty;
    }
}
