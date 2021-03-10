using System.Net;
using System.Threading.Tasks;
using People.Api.Infrastructure.IpAddress;
using People.Domain.AggregateModels.Account;
using People.Infrastructure.Countries;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Infrastructure.Timezones;
using Timezone = People.Domain.AggregateModels.Account.Timezone;

namespace People.Api.Application.IntegrationEventHandlers
{
    public sealed class AccountInfoEventHandler : IKafkaHandler<AccountInfoReceivedIntegrationEvent>
    {
        private readonly ICountryService _country;
        private readonly IIpAddressHasher _ipAddressHasher;
        private readonly IAccountRepository _repository;
        private readonly ITimezoneService _timezone;

        public AccountInfoEventHandler(IAccountRepository repository, ICountryService country,
            ITimezoneService timezone, IIpAddressHasher ipAddressHasher)
        {
            _repository = repository;
            _country = country;
            _timezone = timezone;
            _ipAddressHasher = ipAddressHasher;
        }

        public async Task HandleAsync(AccountInfoReceivedIntegrationEvent message)
        {
            var account = await _repository.GetAsync(message.AccountId);
            if (account is null)
                return;

            var countryCode = await GetCountryCode(message.CountryCode);

            account.SetAddress(new Address(countryCode, message.City ?? account.Address.City));
            account.SetRegistration(IPAddress.Parse(message.Ip), countryCode, _ipAddressHasher.CreateHash);

            if (message.Timezone is not null)
            {
                var timezone = await _timezone.GetAsync(message.Timezone);
                if (timezone is not null && account.Timezone == Timezone.Default)
                    account.SetTimezone(new Timezone(timezone.Name, timezone.Offset));
            }

            account.SetName(account.Name with
            {
                FirstName = account.Name.FirstName ?? message.FirstName,
                LastName = account.Name.LastName ?? message.FirstName
            });

            account.SetProfile(account.Profile with
            {
                Bio = account.Profile.Bio ?? message.AboutMe,
                Picture = account.Profile.Picture == Profile.DefaultPicture
                    ? message.Image ?? account.Profile.Picture
                    : account.Profile.Picture
            });

            await _repository.UpdateAsync(account);
        }

        private async Task<CountryCode> GetCountryCode(string? countryCode)
        {
            if (countryCode is null)
                return CountryCode.Empty;

            var country = await _country.GetAsync(countryCode);
            return country is null
                ? CountryCode.Empty
                : new CountryCode(country.Alpha2Code);
        }
    }
}
