using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class ChangePictureCommand : IRequest
    {
        public ChangePictureCommand(AccountId accountId, Uri? picture)
        {
            AccountId = accountId;
            Picture = picture;
        }

        public AccountId AccountId { get; }

        public Uri? Picture { get; }
    }

    public class ChangePictureCommandHandler : IRequestHandler<ChangePictureCommand>
    {
        private readonly IElwarkStorageClient _client;
        private readonly IAccountRepository _repository;

        public ChangePictureCommandHandler(IAccountRepository repository, IElwarkStorageClient client)
        {
            _repository = repository;
            _client = client;
        }

        public async Task<Unit> Handle(ChangePictureCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            account.SetPicture(request.Picture ?? _client.Static.Icons.User.Default.Path);

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}