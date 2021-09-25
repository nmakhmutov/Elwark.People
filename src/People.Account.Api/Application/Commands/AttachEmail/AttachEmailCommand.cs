using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.AttachEmail
{
    public sealed record AttachEmailCommand(AccountId Id, Identity.Email Email) : IRequest;

    internal sealed class AttachEmailCommandHandler : IRequestHandler<AttachEmailCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;
        
        public AttachEmailCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(AttachEmailCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.AddEmail(request.Email, false, DateTime.UtcNow);

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }
    }
}
