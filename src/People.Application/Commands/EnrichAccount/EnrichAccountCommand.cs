using System.Net;
using Mediator;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Confirmation;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Domain.Repositories;
using People.Domain.ValueObjects;

namespace People.Application.Commands.EnrichAccount;

public sealed record EnrichAccountCommand(long AccountId, string IpAddress) : ICommand;

public sealed class EnrichAccountCommandHandler : ICommandHandler<EnrichAccountCommand>
{
    private readonly IConfirmationChallengeService _confirmation;
    private readonly IGravatarService _gravatar;
    private readonly IEnumerable<IIpService> _ipServices;
    private readonly ILogger<EnrichAccountCommandHandler> _logger;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EnrichAccountCommandHandler(
        IConfirmationChallengeService confirmation,
        IGravatarService gravatar,
        IEnumerable<IIpService> ipServices,
        IAccountRepository repository,
        TimeProvider timeProvider,
        ILogger<EnrichAccountCommandHandler> logger
    )
    {
        _confirmation = confirmation;
        _gravatar = gravatar;
        _repository = repository;
        _ipServices = ipServices;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(EnrichAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.AccountId, ct);

        if (account is null)
            return Unit.Value;

        var ip = await GetIpInformation(request.IpAddress, account.Language);
        var gravatar = await _gravatar.GetAsync(account.GetPrimaryEmail());

        account.Update(
            Name.Create(
                account.Name.Nickname,
                account.Name.FirstName ?? gravatar?.Name?.FirstOrDefault()?.FirstName,
                account.Name.LastName ?? gravatar?.Name?.FirstOrDefault()?.LastName,
                account.Name.UseNickname
            ),
            Picture.Parse(gravatar?.ThumbnailUrl ?? string.Empty),
            account.Language,
            account.Region.IsEmpty() ? RegionCode.ParseOrDefault(ip?.Region) : account.Region,
            account.Country.IsEmpty() ? CountryCode.ParseOrDefault(ip?.CountryCode) : account.Country,
            Timezone.ParseOrDefault(ip?.TimeZone),
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            _timeProvider
        );

        await _confirmation.DeleteByAccountAsync(request.AccountId, ct);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return Unit.Value;
    }

    private async Task<IpInformation?> GetIpInformation(string ip, Language language)
    {
        if (!IPAddress.TryParse(ip, out _))
            return null;

        foreach (var ipService in _ipServices)
        {
            try
            {
                var result = await ipService.GetAsync(ip, language.ToString());
                if (result is null)
                    continue;

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
