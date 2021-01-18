using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record SignInByGoogleCommand(GoogleIdentity Identity, IPAddress Ip) : IRequest<SignInResult>;

    public sealed class SignInByGoogleCommandHandler : IRequestHandler<SignInByGoogleCommand, SignInResult>
    {
        private readonly IAccountRepository _repository;

        public SignInByGoogleCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<SignInResult> Handle(SignInByGoogleCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Identity, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (account.Ban is not null)
                throw new AccountBannedException(account.Ban);

            if (!account.IsConfirmed(request.Identity))
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            account.SignInSuccess(DateTime.UtcNow, request.Ip);
            await _repository.UpdateAsync(account, ct);

            return new SignInResult(account.Id, account.Name.FullName());
        }
    }
}