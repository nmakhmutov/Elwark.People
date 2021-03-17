using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.Exceptions;
using People.Infrastructure.Timezones;
using Timezone = People.Domain.AggregateModels.Account.Timezone;

namespace People.Api.Application.Commands
{
    public sealed record UpdateProfileCommand(
        AccountId Id,
        string? FirstName,
        string? LastName,
        string Nickname,
        string? Bio,
        DateTime Birthday,
        Gender Gender,
        string Language,
        string Timezone,
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

            account.SetName(new Name(request.Nickname, request.FirstName, request.LastName));
            account.SetAddress(new Address(new CountryCode(request.CountryCode), request.City));
            account.SetTimezone(new Timezone(timezone.Name, timezone.Offset));
            account.SetProfile(account.Profile with
            {
                Bio = request.Bio,
                Birthday = request.Birthday.Date,
                Gender = request.Gender,
                Language = new Language(request.Language)
            });

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
