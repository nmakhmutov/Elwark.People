using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class UpdateAccountCommand : UpdateAccountRequest, IRequest
    {
        [DebuggerStepThrough]
        public UpdateAccountCommand(AccountId accountId, string language, Gender gender, DateTime? birthdate,
            string? firstName, string? lastName, string nickname, Uri picture, string timezone, string countryCode,
            string? city, string? bio, IDictionary<LinksType, Uri?> links)
            : base(language, gender, birthdate, firstName, lastName, nickname, picture, timezone, countryCode, city,
                bio, links) =>
            AccountId = accountId;

        public AccountId AccountId { get; }
    }

    public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
    {
        private readonly IAccountRepository _accountRepository;

        public UpdateAccountCommandHandler(IAccountRepository accountRepository) =>
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));

        public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            account.SetName(
                new Name(request.Nickname,
                    request.FirstName.NullIfEmpty(),
                    request.LastName.NullIfEmpty()
                )
            );

            account.SetAddress(new Address(request.CountryCode, request.City.NullIfEmpty()));
            account.SetBasicInfo(
                new BasicInfo(
                    new CultureInfo(request.Language),
                    request.Gender,
                    request.Timezone,
                    request.Birthdate,
                    request.Bio.NullIfEmpty()
                )
            );
            account.SetPicture(request.Picture);
            account.SetLinks(
                new Links(
                    request.Links.Where(x => x.Value is not null)
                        .ToDictionary(x => x.Key, x => x.Value)
                )
            );

            _accountRepository.Update(account);
            await _accountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}