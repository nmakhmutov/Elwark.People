using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;

namespace People.Api.Application.Commands.SignUp
{
    public sealed record SignUpByGoogleCommand(
            GoogleIdentity Google,
            EmailIdentity Email,
            string? FirstName,
            string? LastName,
            Uri? Picture,
            bool IsEmailVerified,
            Language Language,
            IPAddress Ip
        )
        : IRequest<SignUpResult>;

    internal sealed class SignUpByGoogleCommandHandler : IRequestHandler<SignUpByGoogleCommand, SignUpResult>
    {
        private readonly IAccountRepository _repository;

        public SignUpByGoogleCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<SignUpResult> Handle(SignUpByGoogleCommand request, CancellationToken ct)
        {
            var mailAddress = request.Email.GetMailAddress();

            var name = new Name(mailAddress.User, request.FirstName, request.LastName);
            var picture = request.Picture ?? Profile.DefaultPicture;

            var account = new Account(name, request.Language, picture, request.Ip);
            account.AddEmail(mailAddress, request.IsEmailVerified);
            account.AddGoogle(request.Google, name.FullName());

            await _repository.CreateAsync(account, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), null);
        }
    }
}
