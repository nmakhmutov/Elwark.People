using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Password;
using People.Domain;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUp
{
    public sealed record SignUpByEmailCommand(EmailIdentity Email, string Password, Language Language, IPAddress Ip)
        : IRequest<SignUpResult>;

    internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
    {
        private readonly IMediator _mediator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountRepository _repository;
        private readonly ISequenceGenerator _generator;

        public SignUpByEmailCommandHandler(IAccountRepository repository, IPasswordHasher passwordHasher,
            IMediator mediator, ISequenceGenerator generator)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _mediator = mediator;
            _generator = generator;
        }

        public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
        {
            var exists = await _repository.GetAsync(request.Email, ct);

            if (exists is null)
                return await CreateAsync(request, ct);
            
            if (exists.IsConfirmed())
                throw new ElwarkException(ElwarkExceptionCodes.EmailAlreadyExists);

            var confirmationId = await _mediator.Send(
                new SendConfirmationCommand(exists.Id, exists.GetPrimaryEmail().GetIdentity(), request.Language),
                ct
            );

            return new SignUpResult(exists.Id, exists.Name.FullName(), confirmationId);
        }

        private async Task<SignUpResult> CreateAsync(SignUpByEmailCommand request, CancellationToken ct)
        {
            var email = request.Email.GetMailAddress();
            var id = await _generator.NextAccountIdAsync(ct);
            var account = new Account(id, new Name(email.User), request.Language, Account.DefaultPicture, request.Ip);
            account.AddEmail(email, false);

            var salt = _passwordHasher.CreateSalt();
            account.SetPassword(request.Password, salt, _passwordHasher.CreateHash);

            await _repository.CreateAsync(account, ct);

            var confirmation =
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().GetIdentity(), request.Language); 
            
            var confirmationId = await _mediator.Send(confirmation, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), confirmationId);
        }
    }
}
