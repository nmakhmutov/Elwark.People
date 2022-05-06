using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using TimeZone = People.Domain.Aggregates.AccountAggregate.TimeZone;

namespace People.Api.Application.Commands.UpdateAccount;

public sealed record UpdateAccountCommand(
    AccountId Id,
    string? FirstName,
    string? LastName,
    string Nickname,
    bool PreferNickname,
    string Language,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek WeekStart,
    string CountryCode
) : IRequest;

internal sealed class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public UpdateAccountCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
                      ?? throw new PeopleException(ExceptionCodes.AccountNotFound);

        var name = new Name(request.Nickname, request.FirstName, request.LastName, request.PreferNickname);
        account.Update(
            name,
            CountryCode.TryParse(request.CountryCode, out var countryCode) ? countryCode : CountryCode.Empty,
            TimeZone.TryParse(request.TimeZone, out var timeZone) ? timeZone : TimeZone.Utc,
            DateFormat.TryParse(request.DateFormat, out var dateFormat) ? dateFormat : DateFormat.Default,
            TimeFormat.TryParse(request.TimeFormat, out var timeFormat) ? timeFormat : TimeFormat.Default,
            request.WeekStart,
            Language.TryParse(request.Language, out var language) ? language : Language.Default,
            account.Picture
        );

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
