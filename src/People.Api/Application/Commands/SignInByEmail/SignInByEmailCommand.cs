using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByEmail;

public sealed record SignInByEmailCommand(EmailIdentity Email, string Password, IPAddress Ip)
    : IRequest<SignInResult>;

public sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IPasswordHasher _hasher;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

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
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.SignIn(request.Email, DateTime.UtcNow, request.Ip, request.Password, _hasher);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return new SignInResult(account.Id, account.Name.FullName());
    }
}
