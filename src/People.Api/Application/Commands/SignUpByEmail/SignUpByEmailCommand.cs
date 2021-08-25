using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Commands.SendConfirmation;
using People.Api.Application.Models;
using People.Api.Infrastructure.Password;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Sequences;

namespace People.Api.Application.Commands.SignUpByEmail
{
    public sealed record SignUpByEmailCommand(Identity.Email Email, string Password, Language Language, IPAddress Ip)
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
                new SendConfirmationCommand(exists.Id, exists.GetPrimaryEmail().Identity, request.Language),
                ct
            );

            return new SignUpResult(exists.Id, exists.Name.FullName(), confirmationId);
        }

        private async Task<SignUpResult> CreateAsync(SignUpByEmailCommand request, CancellationToken ct)
        {
            var nickname = new MailAddress(request.Email.Value).User;
            var id = await _generator.NextAccountIdAsync(ct);
            var account = new Account(id, new Name(nickname), request.Language, Account.DefaultPicture, request.Ip);
            account.AddEmail(request.Email, false);

            account.SetPassword(request.Password, _passwordHasher);

            await _repository.CreateAsync(account, ct);

            var confirmation =
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().Identity, request.Language); 
            
            var confirmationId = await _mediator.Send(confirmation, ct);

            return new SignUpResult(account.Id, account.Name.FullName(), confirmationId);
        }
    }
}
