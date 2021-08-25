using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUpByMicrosoft
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

        public SignUpByMicrosoftCommandHandler(IAccountRepository repository, ISequenceGenerator generator)
        {
            _repository = repository;
            _generator = generator;
        }

        public async Task<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
        {
            var nickname = new MailAddress(request.Email.Value).User;
            var name = new Name(nickname, request.FirstName, request.LastName);

            var id = await _generator.NextAccountIdAsync(ct);
            var account = new Account(id, name, request.Language, Account.DefaultPicture, request.Ip);
            account.AddEmail(request.Email, true);
            account.AddMicrosoft(request.Identity, request.FirstName, request.LastName);

            await _repository.CreateAsync(account, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), null);
        }
    }
}
