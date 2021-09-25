using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.SetAsPrimaryEmail
{
    internal sealed record SetAsPrimaryEmailCommand(AccountId Id, Identity.Email Email) : IRequest;

    internal sealed class SetAsPrimaryEmailCommandHandler : IRequestHandler<SetAsPrimaryEmailCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public SetAsPrimaryEmailCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(SetAsPrimaryEmailCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.SetAsPrimaryEmail(request.Email);

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }
    }
}
