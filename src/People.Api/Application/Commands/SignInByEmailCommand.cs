using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Password;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record SignInByEmailCommand(EmailIdentity Email, string Password, IPAddress Ip) 
        : IRequest<SignInResult>;

    public sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
    {
        private readonly IAccountRepository _repository;
        private readonly IPasswordHasher _hasher;

        public SignInByEmailCommandHandler(IAccountRepository repository, IPasswordHasher hasher)
        {
            _repository = repository;
            _hasher = hasher;
        }

        public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Email, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (account.Ban is not null)
                throw new AccountBannedException(account.Ban);

            if (!account.IsConfirmed(request.Email))
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            if (!account.IsPasswordAvailable())
                throw new ElwarkException(ElwarkExceptionCodes.PasswordNotCreated);

            if (!account.IsPasswordEqual(request.Password, _hasher.CreateHash))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordMismatch);

            account.SignInSuccess(DateTime.UtcNow, request.Ip);
            await _repository.UpdateAsync(account, ct);

            return new SignInResult(account.Id, account.Name.FullName());
        }
    }
}