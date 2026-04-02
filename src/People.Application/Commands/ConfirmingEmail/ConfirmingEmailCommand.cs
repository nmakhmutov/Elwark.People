using System.Net.Mail;
using Mediator;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ConfirmingEmail;

public sealed record ConfirmingEmailCommand(AccountId Id, MailAddress Email) : ICommand<ConfirmingTokenModel>;

public sealed class ConfirmingEmailCommandHandler : ICommandHandler<ConfirmingEmailCommand, ConfirmingTokenModel>
{
    private readonly IConfirmationService _confirmation;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;

    public ConfirmingEmailCommandHandler(
        IConfirmationService confirmation,
        INotificationSender notification,
        IAccountRepository repository
    )
    {
        _confirmation = confirmation;
        _notification = notification;
        _repository = repository;
    }

    public async ValueTask<ConfirmingTokenModel> Handle(ConfirmingEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        var emailAccount = account.Emails
            .FirstOrDefault(x => x.Email == request.Email.Address) ?? throw EmailException.NotFound(request.Email);

        if (emailAccount.IsConfirmed)
            throw EmailException.AlreadyConfirmed(request.Email);

        var confirmation = await _confirmation.VerifyEmailAsync(account.Id, request.Email, ct);

        await _notification.SendConfirmationAsync(request.Email, confirmation.Code, account.Language, ct);

        return new ConfirmingTokenModel(confirmation.Token);
    }
}
