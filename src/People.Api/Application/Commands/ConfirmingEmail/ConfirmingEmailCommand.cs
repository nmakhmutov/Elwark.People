using System.Net.Mail;
using MediatR;
using People.Api.Infrastructure.Notifications;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmingEmail;

internal sealed record ConfirmingEmailCommand(long Id, MailAddress Email) : IRequest<string>;

internal sealed class ConfirmingEmailCommandHandler : IRequestHandler<ConfirmingEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public ConfirmingEmailCommandHandler(IConfirmationService confirmation, INotificationSender notification,
        IAccountRepository repository, ITimeProvider time)
    {
        _confirmation = confirmation;
        _notification = notification;
        _repository = repository;
        _time = time;
    }

    public async Task<string> Handle(ConfirmingEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);
        var emailAccount = account.Emails.FirstOrDefault(x => x.Email == request.Email.Address)
                    ?? throw EmailException.NotFound(request.Email);

        var email = new MailAddress(emailAccount.Email);
        
        if(emailAccount.IsConfirmed)
            throw EmailException.AlreadyConfirmed(email);
        
        var confirmation = await _confirmation.CreateEmailVerifyAsync(account.Id, email, _time);
        await _notification.SendConfirmationAsync(email, confirmation.Code, account.Language, ct);

        return confirmation.Token;
    }
}
