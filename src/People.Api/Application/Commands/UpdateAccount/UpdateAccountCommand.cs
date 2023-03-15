using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using TimeZone = People.Domain.AggregatesModel.AccountAggregate.TimeZone;

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
    private readonly ITimeProvider _time;

    public UpdateAccountCommandHandler(IAccountRepository repository, ITimeProvider time)
    {
        _repository = repository;
        _time = time;
    }

    public async Task<Account> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.Update(request.Nickname, request.FirstName, request.LastName, request.PreferNickname, _time);
        account.Update(request.Country, _time);
        account.Update(request.TimeZone, _time);
        account.Update(request.DateFormat, _time);
        account.Update(request.TimeFormat, _time);
        account.Update(request.Language, _time);
        account.Update(request.StartOfWeek, _time);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return account;
    }
}
