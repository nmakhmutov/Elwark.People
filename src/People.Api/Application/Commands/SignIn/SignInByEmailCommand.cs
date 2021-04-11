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

namespace People.Api.Application.Commands.SignIn
{
    public sealed record SignInByEmailCommand(EmailIdentity Email, string Password, IPAddress Ip)
        : IRequest<SignInResult>;

    public sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
    {
        private readonly IPasswordHasher _hasher;
        private readonly IAccountRepository _repository;

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

            account.SignIn(request.Email, DateTime.UtcNow, request.Ip, request.Password, _hasher.CreateHash);

            await _repository.UpdateAsync(account, ct);

            return new SignInResult(account.Id, account.Name.FullName());
        }
    }
}
