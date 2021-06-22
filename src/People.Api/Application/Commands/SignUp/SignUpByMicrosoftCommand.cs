using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUp
{
    public sealed record SignUpByMicrosoftCommand(
        MicrosoftIdentity Identity,
        EmailIdentity Email,
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
            var email = request.Email.GetMailAddress();
            var name = new Name(email.User, request.FirstName, request.LastName);

            var id = await _generator.NextAccountIdAsync(ct);
            var account = new Account(id, name, request.Language, Account.DefaultPicture, request.Ip);
            account.AddEmail(email, true);
            account.AddMicrosoft(request.Identity, name.FullName());

            await _repository.CreateAsync(account, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), null);
        }
    }
}
