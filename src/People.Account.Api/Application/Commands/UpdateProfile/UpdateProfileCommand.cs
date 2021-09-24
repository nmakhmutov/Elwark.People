using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.UpdateProfile
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
        string TimeZone,
        DayOfWeek FirstDayOfWeek,
        string CountryCode,
        string City
    ) : IRequest;

    internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public UpdateProfileCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.Update(
                new Name(request.Nickname, request.FirstName, request.LastName, request.PreferNickname),
                new Address(new CountryCode(request.CountryCode), request.City),
                request.TimeZone,
                request.FirstDayOfWeek,
                new Language(request.Language),
                request.Gender,
                account.Picture,
                request.Bio,
                request.DateOfBirth
            );

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }
    }
}
