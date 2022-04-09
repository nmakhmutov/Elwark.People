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
using People.Domain.Exceptions;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUpByEmail;

public sealed record SignUpByEmailCommand(EmailIdentity Email, string Password, Language Language, IPAddress Ip)
    : IRequest<SignUpResult>;

internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
{
    private readonly ISequenceGenerator _generator;
    private readonly IMediator _mediator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountRepository _repository;

    public SignUpByEmailCommandHandler(IAccountRepository repository, IPasswordHasher passwordHasher,
        ISequenceGenerator generator, IMediator mediator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _generator = generator;
        _mediator = mediator;
    }

    public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Email, ct)
                      ?? await CreateAsync(request, ct);

        if (account.IsActivated)
            throw new PeopleException(ExceptionCodes.EmailAlreadyExists);

        return new SignUpResult(account.Id, account.Name.FullName(), account.GetPrimaryEmail());
    }

    private async Task<Account> CreateAsync(SignUpByEmailCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var name = new Name(new MailAddress(request.Email.Value).User);
        var id = await _generator.NextAccountIdAsync(ct);

        var account = new Account(id, name, request.Language, Account.DefaultPicture, request.Ip);
        account.AddIdentity(request.Email, false, now);
        account.SetPassword(request.Password, _passwordHasher, now);

        await _repository.CreateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return account;
    }
}
