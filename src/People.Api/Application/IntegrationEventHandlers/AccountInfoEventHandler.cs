using System.Net;
using System.Threading.Tasks;
using Common.Kafka;
using Integration.Event;
using People.Domain.Aggregates.AccountAggregate;
using People.Infrastructure.Countries;

namespace People.Api.Application.IntegrationEventHandlers;

public sealed class AccountInfoEventHandler : IKafkaHandler<AccountInfoReceivedIntegrationEvent>
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
        account.SetRegistration(IPAddress.Parse(message.Ip), countryCode, _ipHasher);

        account.Update(account.Name with
            {
                FirstName = account.Name.FirstName ?? message.FirstName?[..Name.FirstNameLength],
                LastName = account.Name.LastName ?? message.FirstName?[..Name.LastNameLength]
            },
            countryCode,
            message.TimeZone ?? account.TimeZone,
            account.FirstDayOfWeek,
            account.Language,
            account.Picture == Account.DefaultPicture
                ? message.Image ?? account.Picture
                : account.Picture
        );

        await _repository.UpdateAsync(account);
    }

    private async Task<CountryCode> GetCountryCodeAsync(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return CountryCode.Empty;

        var country = await _country.GetAsync(code);
        return country is null
            ? CountryCode.Empty
            : new CountryCode(country.Alpha2Code);
    }
}
