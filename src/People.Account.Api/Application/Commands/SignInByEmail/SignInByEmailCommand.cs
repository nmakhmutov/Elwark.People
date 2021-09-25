using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Application.Models;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.SignInByEmail
{
    public sealed record SignInByEmailCommand(Identity.Email Email, string Password, IPAddress Ip)
        : IRequest<SignInResult>;

    public sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
    {
        private readonly IPasswordHasher _hasher;
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public SignInByEmailCommandHandler(IAccountRepository repository, IPasswordHasher hasher, IMediator mediator)
        {
            _repository = repository;
            _hasher = hasher;
            _mediator = mediator;
        }

        public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Email, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.SignIn(request.Email, DateTime.UtcNow, request.Ip, request.Password, _hasher);

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return new SignInResult(account.Id, account.Name.FullName());
        }
    }
}
