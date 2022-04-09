using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Blacklist;

namespace People.Api.Application.Commands.AttachMicrosoft;

public sealed record AttachMicrosoftCommand(
    AccountId Id,
    Identity.Microsoft Microsoft,
    Identity.Email Email,
    string? FirstName,
    string? LastName
) : IRequest;

internal sealed class AttachMicrosoftCommandHandler : IRequestHandler<AttachMicrosoftCommand>
{
    private readonly IBlacklistService _blacklist;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public AttachMicrosoftCommandHandler(IAccountRepository repository, IBlacklistService blacklist, IMediator mediator)
    {
        _repository = repository;
        _blacklist = blacklist;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(AttachMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        var now = DateTime.UtcNow;
        account.AddMicrosoft(request.Microsoft, request.FirstName, request.LastName, now);
        if (await IsAvailableToAttach(request.Email, ct))
            account.AddEmail(request.Email, false, now);

        account.Update(
            account.Name with
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
        return !await _blacklist.IsEmailHostDenied(host, ct);
    }
}
