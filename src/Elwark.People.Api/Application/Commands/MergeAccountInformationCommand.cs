using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class MergeAccountInformationCommand : IRequest
    {
        public MergeAccountInformationCommand(AccountId accountId, string? firstName, string? lastName, Uri? picture,
            string? timezone, string? countryCode, string? city, string? bio, Gender? gender, DateTime? birthday)
        {
            AccountId = accountId;
            FirstName = firstName;
            LastName = lastName;
            Picture = picture;
            Timezone = timezone;
            CountryCode = countryCode;
            City = city;
            Bio = bio;
            Gender = gender;
            Birthday = birthday;
        }

        public AccountId AccountId { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public Uri? Picture { get; }

        public string? Timezone { get; }

        public string? CountryCode { get; }

        public string? City { get; }

        public string? Bio { get; }

        public Gender? Gender { get; }

        public DateTime? Birthday { get; }
    }

    public class AppendAccountCommandHandler : IRequestHandler<MergeAccountInformationCommand>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IElwarkStorageClient _storageClient;

        public AppendAccountCommandHandler(IAccountRepository accountRepository, IElwarkStorageClient storageClient)
        {
            _accountRepository = accountRepository;
            _storageClient = storageClient;
        }

        public async Task<Unit> Handle(MergeAccountInformationCommand request, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            account.SetName(new Name(
                account.Name.Nickname,
                account.Name.FirstName ?? request.FirstName.NullIfEmpty(),
                account.Name.LastName ?? request.LastName.NullIfEmpty()
            ));

            account.SetAddress(new Address(
                account.Address.CountryCode ?? request.CountryCode.NullIfEmpty(),
                account.Address.City ?? request.City.NullIfEmpty()
            ));

            account.SetBasicInfo(new BasicInfo(
                account.BasicInfo.Language,
                request.Gender ?? account.BasicInfo.Gender,
                request.Timezone ?? account.BasicInfo.Timezone,
                account.BasicInfo.Birthday ?? request.Birthday,
                account.BasicInfo.Bio ?? request.Bio.NullIfEmpty()
            ));

            if (account.Picture == _storageClient.Static.Icons.User.Default.Path && request.Picture is {})
                account.SetPicture(request.Picture);

            _accountRepository.Update(account);
            await _accountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}