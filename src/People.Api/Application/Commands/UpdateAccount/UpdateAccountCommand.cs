using MediatR;
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
    private readonly IWorldClient _worldClient;

    public UpdateAccountCommandHandler(IAccountRepository repository, IWorldClient worldClient)
    {
        _repository = repository;
        _worldClient = worldClient;
    }

    public async Task<Account> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        var region = await GetRegionAsync(request.Country, ct);

        account.Update(request.Nickname, request.FirstName, request.LastName, request.PreferNickname);
        account.Update(request.DateFormat, request.TimeFormat, request.StartOfWeek);
        account.Update(request.Language, region, request.Country, request.TimeZone);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return account;
    }

    private async Task<RegionCode> GetRegionAsync(CountryCode country, CancellationToken ct)
    {
        var result = await _worldClient.GetCountryAsync(country, ct);
        return result is null ? RegionCode.Empty : RegionCode.Parse(result.Region);
    }
}
