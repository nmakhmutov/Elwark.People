using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Application.Models;
using People.Account.Api.Infrastructure;
using People.Account.Domain;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Infrastructure.Sequences;

namespace People.Account.Api.Application.Commands.SignUpByMicrosoft
{
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
        private readonly IAccountRepository _repository;
        private readonly ISequenceGenerator _generator;
        private readonly IMediator _mediator;

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
            var account = new Domain.Aggregates.AccountAggregate.Account(id, name, request.Language,
                Domain.Aggregates.AccountAggregate.Account.DefaultPicture, request.Ip);
            var email = account.AddEmail(request.Email, true, now);
            account.AddMicrosoft(request.Identity, request.FirstName, request.LastName, now);

            await _repository.CreateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return new SignUpResult(account.Id, account.Name.FullName(), email);
        }
    }
}
