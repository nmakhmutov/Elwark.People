using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Api.Application.Models;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignInByMicrosoft;

public sealed record SignInByMicrosoftCommand(Identity.Microsoft Identity, IPAddress Ip) : IRequest<SignInResult>;

public sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public SignInByMicrosoftCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Identity, ct);
        if (account is null)
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

        account.SignIn(request.Identity, DateTime.UtcNow, request.Ip);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return new SignInResult(account.Id, account.Name.FullName());
    }
}
