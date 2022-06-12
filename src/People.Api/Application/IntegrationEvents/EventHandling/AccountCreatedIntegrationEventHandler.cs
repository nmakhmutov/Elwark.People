using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Api.Infrastructure.Providers.IpApi;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.SeedWork;
using People.Infrastructure.Kafka;
using TimeZone = People.Domain.AggregatesModel.AccountAggregate.TimeZone;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountCreatedIntegrationEventHandler : IKafkaHandler<AccountCreatedIntegrationEvent>
{
    private readonly IGravatarService _gravatar;
    private readonly IIpApiService _ipService;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public AccountCreatedIntegrationEventHandler(IGravatarService gravatar, IIpApiService ipService,
        IAccountRepository repository, ITimeProvider time)
    {
        _gravatar = gravatar;
        _ipService = ipService;
        _repository = repository;
        _time = time;
    }

    public async Task HandleAsync(AccountCreatedIntegrationEvent message)
    {
        var account = await _repository.GetAsync(message.Id);
        if (account is null)
            return;

        var changed = false;
        var ipInformation = await _ipService.GetAsync(message.Ip, account.Language.ToString());
        var gravatar = await _gravatar.GetAsync(account.GetPrimaryEmail());

        if (ipInformation is not null)
        {
            var country = CountryCode.TryParse(ipInformation.CountryCode, out var x) ? x : CountryCode.Empty;
            account.Update(country, _time);
            account.UpdateRegistrationCountry(country);
            
            if (TimeZone.TryParse(ipInformation.TimeZone, out var timeZone))
                account.Update(timeZone, _time);
            
            changed = true;
        }

        if (gravatar is not null)
        {
            var firstName = gravatar.Name?.FirstOrDefault()?.FirstName ?? account.Name.FirstName;
            var lastName = gravatar.Name?.FirstOrDefault()?.LastName ?? account.Name.LastName;
            account.Update(firstName, lastName, _time);

            if (Uri.TryCreate(gravatar.ThumbnailUrl, UriKind.Absolute, out var image))
                account.Update(image, _time);

            changed = true;
        }

        if (changed)
        {
            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync();
        }
    }
}
