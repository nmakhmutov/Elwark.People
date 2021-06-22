using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignIn
{
    public sealed record SignInByMicrosoftCommand(MicrosoftIdentity Identity, IPAddress Ip) : IRequest<SignInResult>;

    public sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
    {
        private readonly IAccountRepository _repository;

        public SignInByMicrosoftCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Identity, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.SignIn(request.Identity, DateTime.UtcNow, request.Ip);

            await _repository.UpdateAsync(account, ct);

            return new SignInResult(account.Id, account.Name.FullName());
        }
    }
}
