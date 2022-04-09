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

namespace People.Api.Application.Commands.SignUpByGoogle;

public sealed record SignUpByGoogleCommand(
    Identity.Google Google,
    Identity.Email Email,
    string? FirstName,
    string? LastName,
    Uri? Picture,
    bool IsEmailVerified,
    Language Language,
    IPAddress Ip
) : IRequest<SignUpResult>;

internal sealed class SignUpByGoogleCommandHandler : IRequestHandler<SignUpByGoogleCommand, SignUpResult>
{
    private readonly ISequenceGenerator _generator;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public SignUpByGoogleCommandHandler(IAccountRepository repository, ISequenceGenerator generator, IMediator mediator)
    {
        _repository = repository;
        _generator = generator;
        _mediator = mediator;
    }

    public async Task<SignUpResult> Handle(SignUpByGoogleCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nickname = new MailAddress(request.Email.Value).User;

        var name = new Name(nickname, request.FirstName, request.LastName);
        var picture = request.Picture ?? Account.DefaultPicture;

        var id = await _generator.NextAccountIdAsync(ct);
        var account = new Account(id, name, request.Language, picture, request.Ip);
        var email = account.AddEmail(request.Email, request.IsEmailVerified, now);
        account.AddGoogle(request.Google, request.FirstName, request.LastName, now);
        if (account.IsConfirmed())
            account.SignIn(new Identity.Google(request.Google.Value), now, request.Ip);

        await _repository.CreateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return new SignUpResult(account.Id, account.Name.FullName(), email);
    }
}
