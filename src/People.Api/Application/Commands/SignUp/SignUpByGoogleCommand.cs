using System;
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
    public sealed record SignUpByGoogleCommand(
        GoogleIdentity Google,
        EmailIdentity Email,
        string? FirstName,
        string? LastName,
        Uri? Picture,
        bool IsEmailVerified,
        Language Language,
        IPAddress Ip
    ) : IRequest<SignUpResult>;

    internal sealed class SignUpByGoogleCommandHandler : IRequestHandler<SignUpByGoogleCommand, SignUpResult>
    {
        private readonly IAccountRepository _repository;
        private readonly ISequenceGenerator _generator;

        public SignUpByGoogleCommandHandler(IAccountRepository repository, ISequenceGenerator generator)
        {
            _repository = repository;
            _generator = generator;
        }

        public async Task<SignUpResult> Handle(SignUpByGoogleCommand request, CancellationToken ct)
        {
            var mailAddress = request.Email.GetMailAddress();

            var name = new Name(mailAddress.User, request.FirstName, request.LastName);
            var picture = request.Picture ?? Account.DefaultPicture;

            var id = await _generator.NextAccountIdAsync(ct);
            var account = new Account(id, name, request.Language, picture, request.Ip);
            account.AddEmail(mailAddress, request.IsEmailVerified);
            account.AddGoogle(request.Google, request.FirstName, request.LastName);

            await _repository.CreateAsync(account, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), null);
        }
    }
}
