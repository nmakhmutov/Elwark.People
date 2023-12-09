using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Api.Infrastructure.Providers.IpApi;
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
    private readonly IIpApiService _ipService;
    private readonly IAccountRepository _repository;

    public AccountCreatedIntegrationEventHandler(IConfirmationService confirmation, IGravatarService gravatar,
        IIpApiService ipService, IAccountRepository repository)
    {
        _confirmation = confirmation;
        _gravatar = gravatar;
        _ipService = ipService;
        _repository = repository;
    }

    public async Task HandleAsync(AccountCreatedIntegrationEvent message, CancellationToken ct)
    {
        var account = await _repository.GetAsync(message.AccountId, ct);

        if (account is null)
            return;

        var ipInformation = await _ipService.GetAsync(message.Ip, account.Language.ToString());

        if (ipInformation is not null)
        {
            RegionCode.TryParse(ipInformation.ContinentCode, out var region);
            CountryCode.TryParse(ipInformation.CountryCode, out var country);
            TimeZone.TryParse(ipInformation.TimeZone, out var timeZone);

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
}
