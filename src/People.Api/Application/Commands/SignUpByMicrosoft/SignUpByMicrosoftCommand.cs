using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

public sealed record SignUpByMicrosoftCommand(
    MicrosoftIdentity Microsoft,
    EmailIdentity Email,
    string? FirstName,
    string? LastName,
    Language Language,
    IPAddress Ip
) : IRequest<SignUpResult>;

internal sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
{
    private readonly ISequenceGenerator _generator;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public SignUpByMicrosoftCommandHandler(IAccountRepository repository, ISequenceGenerator generator, IMediator mediator)
    {
        _repository = repository;
        _generator = generator;
        _mediator = mediator;
    }

    public async Task<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nickname = new MailAddress(request.Email.Value).User;
        var name = new Name(nickname, request.FirstName, request.LastName);
        var id = await _generator.NextAccountIdAsync(ct);
        
        var account = new Account(id, name, request.Language, Account.DefaultPicture, request.Ip);
        var email = account.AddIdentity(request.Email, true, now);
        account.AddIdentity(request.Microsoft, request.FirstName, request.LastName, now);
        
        if (account.IsActivated)
            account.SignIn(request.Microsoft, now, request.Ip);

        await _repository.CreateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return new SignUpResult(account.Id, account.Name.FullName(), email);
    }
}
