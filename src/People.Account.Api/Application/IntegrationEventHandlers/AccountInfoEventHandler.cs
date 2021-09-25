using System.Net;
using System.Threading.Tasks;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Infrastructure.Countries;
using People.Integration.Event;
using People.Kafka;

namespace People.Account.Api.Application.IntegrationEventHandlers
{
    public sealed class AccountInfoEventHandler : IKafkaHandler<AccountInfoReceivedIntegrationEvent>
    {
        private readonly ICountryService _country;
        private readonly IIpAddressHasher _ipAddressHasher;
        private readonly IAccountRepository _repository;

        public AccountInfoEventHandler(IAccountRepository repository, ICountryService country,
            IIpAddressHasher ipAddressHasher)
        {
            _repository = repository;
            _country = country;
            _ipAddressHasher = ipAddressHasher;
        }

        public async Task HandleAsync(AccountInfoReceivedIntegrationEvent message)
        {
            var account = await _repository.GetAsync(message.AccountId);
            if (account is null)
                return;

            var countryCode = await GetCountryCode(message.CountryCode);
            account.SetRegistration(IPAddress.Parse(message.Ip), countryCode, _ipAddressHasher);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? message.FirstName?[..Name.FirstNameLength],
                    LastName = account.Name.LastName ?? message.FirstName?[..Name.LastNameLength]
                },
                countryCode,
                message.TimeZone ?? account.TimeZone,
                account.FirstDayOfWeek,
                account.Language,
                account.Picture == Domain.Aggregates.AccountAggregate.Account.DefaultPicture
                    ? message.Image ?? account.Picture
                    : account.Picture
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
    }
}
