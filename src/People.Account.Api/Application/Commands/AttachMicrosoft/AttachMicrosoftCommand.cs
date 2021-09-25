using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Api.Infrastructure;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;
using People.Account.Infrastructure.Forbidden;

namespace People.Account.Api.Application.Commands.AttachMicrosoft
{
    public sealed record AttachMicrosoftCommand(
        AccountId Id,
        Identity.Microsoft Microsoft,
        Identity.Email Email,
        string? FirstName,
        string? LastName
    ) : IRequest;

    internal sealed class AttachMicrosoftCommandHandler : IRequestHandler<AttachMicrosoftCommand>
    {
        private readonly IForbiddenService _forbidden;
        private readonly IAccountRepository _repository;
        private readonly IMediator _mediator;

        public AttachMicrosoftCommandHandler(IAccountRepository repository, IForbiddenService forbidden, IMediator mediator)
        {
            _repository = repository;
            _forbidden = forbidden;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(AttachMicrosoftCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            var now = DateTime.UtcNow;
            account.AddMicrosoft(request.Microsoft, request.FirstName, request.LastName, now);
            if (await IsAvailableToAttach(request.Email, ct))
                account.AddEmail(request.Email, false, now);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? request.FirstName,
                    LastName = account.Name.LastName ?? request.LastName
                },
                account.Picture
            );

            await _repository.UpdateAsync(account, ct);
            await _mediator.DispatchDomainEventsAsync(account);
            
            return Unit.Value;
        }

        private async Task<bool> IsAvailableToAttach(Identity.Email email, CancellationToken ct)
        {
            if (await _repository.IsExists(email, ct))
                return false;

            var host = new MailAddress(email.Value).Host;
            if (await _forbidden.IsEmailHostDenied(host, ct))
                return false;

            return true;
        }
    }
}
