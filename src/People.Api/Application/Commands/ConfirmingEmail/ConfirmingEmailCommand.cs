using System.Net.Mail;
using MediatR;
using People.Api.Infrastructure.Notifications;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmingEmail;

internal sealed record ConfirmingEmailCommand(long Id, MailAddress Email) : IRequest<string>;

internal sealed class ConfirmingEmailCommandHandler : IRequestHandler<ConfirmingEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmingEmailCommandHandler(IConfirmationService confirmation, INotificationSender notification,
        IAccountRepository repository, TimeProvider timeProvider)
    {
        _confirmation = confirmation;
        _notification = notification;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<string> Handle(ConfirmingEmailCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        var emailAccount = account.Emails
            .FirstOrDefault(x => x.Email == request.Email.Address) ?? throw EmailException.NotFound(request.Email);

        if (emailAccount.IsConfirmed)
            throw EmailException.AlreadyConfirmed(request.Email);

        var confirmation = await _confirmation.VerifyEmailAsync(account.Id, request.Email, _timeProvider, ct)
            .ConfigureAwait(false);

        await _notification.SendConfirmationAsync(request.Email, confirmation.Code, account.Language, ct)
            .ConfigureAwait(false);

        return confirmation.Token;
    }
}
