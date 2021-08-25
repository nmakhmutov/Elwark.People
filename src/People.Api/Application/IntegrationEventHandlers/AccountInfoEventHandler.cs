using System.Net;
using System.Threading.Tasks;
using People.Domain.Aggregates.AccountAggregate;
using People.Infrastructure.Countries;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Infrastructure.Timezones;
using Timezone = People.Domain.Aggregates.AccountAggregate.Timezone;

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
            var timezone = string.IsNullOrEmpty(message.Timezone)
                ? account.TimeInfo.Timezone
                : await GetTimezoneAsync(message.Timezone);
            account.SetRegistration(IPAddress.Parse(message.Ip), countryCode, _ipAddressHasher);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? message.FirstName?[..Name.FirstNameLength],
                    LastName = account.Name.LastName ?? message.FirstName?[..Name.LastNameLength]
                },
                new Address(countryCode, message.City ?? account.Address.City),
                account.TimeInfo with { Timezone = timezone },
                account.Language,
                account.Gender,
                account.Picture == Account.DefaultPicture
                    ? message.Image ?? account.Picture
                    : account.Picture,
                account.Bio ?? message.AboutMe,
                account.DateOfBirth
            );

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

        private async Task<Timezone> GetTimezoneAsync(string timezone)
        {
            var zone = await _timezone.GetAsync(timezone);
            return zone is null ? Timezone.Default : new Timezone(zone.Name, zone.Offset);
        }
    }
}
