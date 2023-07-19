using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Api.Application.Commands.UpdateAccount;

internal sealed record UpdateAccountCommand(
    long Id,
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

    public UpdateAccountCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task<Account> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.Update(request.Nickname, request.FirstName, request.LastName, request.PreferNickname);
        account.Update(request.DateFormat, request.TimeFormat, request.StartOfWeek);
        account.Update(request.Language, request.Country, request.TimeZone);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return account;
    }
}
