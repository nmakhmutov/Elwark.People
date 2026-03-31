using Mediator;
using People.Api.Infrastructure.Providers.World;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Api.Application.Commands.UpdateAccount;

internal sealed record UpdateAccountCommand(
    AccountId Id,
    string? FirstName,
    string? LastName,
    string Nickname,
    bool PreferNickname,
    Language Language,
    TimeZone TimeZone,
    DateFormat DateFormat,
    TimeFormat TimeFormat,
    DayOfWeek StartOfWeek,
    CountryCode Country
) : IRequest<Account>;

internal sealed class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Account>
{
    private readonly IAccountRepository _repository;
    private readonly ICountryClient _countryClient;
    private readonly TimeProvider _timeProvider;

    public UpdateAccountCommandHandler(
        IAccountRepository repository,
        ICountryClient countryClient,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _countryClient = countryClient;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Account> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        var region = await GetRegionAsync(request.Country, ct);

        account.Update(request.Nickname, request.FirstName, request.LastName, request.PreferNickname, _timeProvider);
        account.Update(request.DateFormat, request.TimeFormat, request.StartOfWeek, _timeProvider);
        account.Update(request.Language, region, request.Country, request.TimeZone, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return account;
    }

    private async Task<RegionCode> GetRegionAsync(CountryCode country, CancellationToken ct)
    {
        var result = await _countryClient.GetAsync(country, ct);
        return result?.Region ?? RegionCode.Empty;
    }
}
