using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Models;
using People.Api.Infrastructure.Password;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SignUp
{
    public sealed record SignUpByEmailCommand(EmailIdentity Email, string Password, Language Language, IPAddress Ip)
        : IRequest<SignUpResult>;

    internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
    {
        private readonly IMediator _mediator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountRepository _repository;

        public SignUpByEmailCommandHandler(IAccountRepository repository, IPasswordHasher passwordHasher,
            IMediator mediator)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _mediator = mediator;
        }

        public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
        {
            var exists = await _repository.GetAsync(request.Email, ct);
            ObjectId confirmationId;

            if (exists is not null)
            {
                if (exists.IsConfirmed())
                    throw new ElwarkException(ElwarkExceptionCodes.EmailAlreadyExists);

                confirmationId = await _mediator.Send(
                    new SendConfirmationCommand(exists.Id, exists.GetPrimaryEmail().GetIdentity(), request.Language),
                    ct
                );

                return new SignUpResult(exists.Id, exists.Name.FullName(), confirmationId);
            }

            var email = request.Email.GetMailAddress();
            var account = new Account(new Name(email.User), request.Language, Profile.DefaultPicture, request.Ip);

            account.AddEmail(email, false);

            var salt = _passwordHasher.CreateSalt();
            account.SetPassword(request.Password, salt, _passwordHasher.CreateHash);

            await _repository.CreateAsync(account, ct);
            confirmationId = await _mediator.Send(
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().GetIdentity(), request.Language),
                ct
            );

            return new SignUpResult(account.Id, account.Name.FullName(), confirmationId);
        }
    }
}
