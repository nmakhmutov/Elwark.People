using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain;
using People.Domain.Aggregates.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Timezones;
using Timezone = People.Domain.Aggregates.Account.Timezone;

namespace People.Api.Application.Commands
{
    public sealed record UpdateProfileCommand(
        AccountId Id,
        string? FirstName,
        string? LastName,
        string Nickname,
        bool PreferNickname,
        string? Bio,
        DateTime DateOfBirth,
        Gender Gender,
        string Language,
        string Timezone,
        DayOfWeek FirstDayOfWeek,
        string CountryCode,
        string City
    ) : IRequest;

    internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly ITimezoneService _timezone;

        public UpdateProfileCommandHandler(IAccountRepository repository, ITimezoneService timezone)
        {
            _repository = repository;
            _timezone = timezone;
        }

        public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var timezone = await _timezone.GetAsync(request.Timezone, ct);
            if (timezone is null)
                throw new ElwarkException(ElwarkExceptionCodes.TimezoneNotFound);

            account.Update(
                new Name(request.Nickname, request.FirstName, request.LastName, request.PreferNickname),
                new Address(new CountryCode(request.CountryCode), request.City),
                new TimeInfo(new Timezone(timezone.Name, timezone.Offset), request.FirstDayOfWeek),
                new Language(request.Language),
                request.Gender,
                account.Picture,
                request.Bio,
                request.DateOfBirth
            );

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
