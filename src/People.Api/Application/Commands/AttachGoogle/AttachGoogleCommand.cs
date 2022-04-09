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

namespace People.Api.Application.Commands.AttachGoogle;

public sealed record AttachGoogleCommand(
    AccountId Id,
    GoogleIdentity Google,
    EmailIdentity Email,
    string? FirstName,
    string? LastName,
    Uri? Picture,
    bool IsEmailVerified
) : IRequest;

internal sealed class AttachGoogleCommandHandler : IRequestHandler<AttachGoogleCommand>
{
    private readonly IBlacklistService _blacklist;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public AttachGoogleCommandHandler(IAccountRepository repository, IBlacklistService blacklist, IMediator mediator)
    {
        _repository = repository;
        _blacklist = blacklist;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(AttachGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        var now = DateTime.UtcNow;
        account.AddIdentity(request.Google, request.FirstName, request.LastName, now);
        if (await IsAvailableToAttach(request.Email, ct))
            account.AddIdentity(request.Email, request.IsEmailVerified, now);

        account.Update(
            account.Name with
            {
                FirstName = account.Name.FirstName ?? request.FirstName?[..Name.FirstNameLength],
                LastName = account.Name.LastName ?? request.LastName?[..Name.LastNameLength]
            },
            request.Picture is not null &&
            account.Picture == Account.DefaultPicture
                ? request.Picture
                : account.Picture
        );

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }

    private async Task<bool> IsAvailableToAttach(EmailIdentity email, CancellationToken ct)
    {
        if (await _repository.IsExists(email, ct))
            return false;

        var host = new MailAddress(email.Value).Host;
        return !await _blacklist.IsEmailHostDenied(host, ct);
    }
}
