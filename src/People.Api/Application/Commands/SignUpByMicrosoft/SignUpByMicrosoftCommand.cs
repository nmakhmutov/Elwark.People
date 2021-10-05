using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

public sealed record SignUpByMicrosoftCommand(
    Identity.Microsoft Identity,
    Identity.Email Email,
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

    public SignUpByMicrosoftCommandHandler(IAccountRepository repository, ISequenceGenerator generator,
        IMediator mediator)
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
        var email = account.AddEmail(request.Email, true, now);
        account.AddMicrosoft(request.Identity, request.FirstName, request.LastName, now);

        await _repository.CreateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return new SignUpResult(account.Id, account.Name.FullName(), email);
    }
}
