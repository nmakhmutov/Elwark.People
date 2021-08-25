using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Commands.SendConfirmation;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(Identity Key, Language Language) : IRequest<AccountId>;

    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AccountId>
    {
        private readonly IMediator _mediator;
        private readonly IAccountRepository _repository;

        public ResetPasswordCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<AccountId> Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Key, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (!account.IsPasswordAvailable())
                throw new ElwarkException(ElwarkExceptionCodes.PasswordNotCreated);

            await _mediator.Send(
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().Identity, request.Language),
                ct
            );

            return account.Id;
        }
    }
}
