using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    AccountId Id,
    string? FirstName,
    string? LastName,
    string Nickname,
    bool PreferNickname,
    string Language,
    string TimeZone,
    DayOfWeek FirstDayOfWeek,
    string CountryCode
) : IRequest;

internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public UpdateProfileCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.Update(
            new Name(request.Nickname, request.FirstName, request.LastName, request.PreferNickname),
            new CountryCode(request.CountryCode),
            request.TimeZone,
            request.FirstDayOfWeek,
            new Language(request.Language),
            account.Picture
        );

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
