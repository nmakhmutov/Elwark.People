using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Password;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands
{
    public sealed record SignUpByEmailCommand(EmailIdentity Email, string Password, Language Language, IPAddress Ip)
        : IRequest<SignUpResult>;

    internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
    {
        private readonly IAccountRepository _repository;
        private readonly IPasswordHasher _hasher;
        private readonly IMediator _mediator;

        public SignUpByEmailCommandHandler(IAccountRepository repository, IPasswordHasher hasher, IMediator mediator)
        {
            _repository = repository;
            _hasher = hasher;
            _mediator = mediator;
        }

        public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
        {
            var exists = await _repository.GetAsync(request.Email, ct);
            if (exists is not null)
            {
                if (exists.IsConfirmed())
                    throw new ElwarkException(ElwarkExceptionCodes.EmailAlreadyExists);

                await _mediator.Send(new SendPrimaryEmailConfirmationCommand(exists.Id, exists.GetPrimaryEmail()), ct);
                return new SignUpResult(exists.Id, exists.Name.FullName(), true);
            }

            var email = request.Email.GetMailAddress();
            var account = new Account(new Name(email.User), request.Language, Profile.DefaultPicture, request.Ip);
            account.AddEmail(email, EmailType.Primary, false);
            
            var salt = _hasher.CreateSalt();
            account.SetPassword(request.Password, salt, _hasher.CreateHash);

            await _repository.CreateAsync(account, ct);
            await _mediator.Send(new SendPrimaryEmailConfirmationCommand(account.Id, account.GetPrimaryEmail()), ct);

            return new SignUpResult(account.Id, account.Name.FullName(), true);
        }
    }
}