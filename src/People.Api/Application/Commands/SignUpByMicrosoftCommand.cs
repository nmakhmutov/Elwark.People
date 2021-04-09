using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;

namespace People.Api.Application.Commands
{
    public sealed record SignUpByMicrosoftCommand(
            MicrosoftIdentity Identity,
            EmailIdentity Email,
            string? FirstName,
            string? LastName,
            Language Language,
            IPAddress Ip
        )
        : IRequest<SignUpResult>;

    internal sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
    {
        private readonly IAccountRepository _repository;

        public SignUpByMicrosoftCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
        {
            var email = request.Email.GetMailAddress();
            var name = new Name(email.User, request.FirstName, request.LastName);

            var account = new Account(name, request.Language, Profile.DefaultPicture, request.Ip);
            account.AddEmail(email, true);
            account.AddMicrosoft(request.Identity, name.FullName());

            await _repository.CreateAsync(account, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), null);
        }
    }
}
