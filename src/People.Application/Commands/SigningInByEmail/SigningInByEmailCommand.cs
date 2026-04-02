using System.Net.Mail;
using Mediator;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;

namespace People.Application.Commands.SigningInByEmail;

public sealed record SigningInByEmailCommand(MailAddress Email, Language Language) : ICommand<string>;

public sealed class SigningInByEmailCommandHandler : ICommandHandler<SigningInByEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;

    public SigningInByEmailCommandHandler(
        IConfirmationService confirmation,
        INotificationSender notification,
        IAccountRepository repository
    )
    {
        _confirmation = confirmation;
        _notification = notification;
        _repository = repository;
    }

    public async ValueTask<string> Handle(SigningInByEmailCommand request, CancellationToken ct)
    {
        var email = await _repository.GetEmailSignupStateAsync(request.Email, ct)
            ?? throw EmailException.NotFound(request.Email);

        if (!email.IsConfirmed)
            throw EmailException.NotConfirmed(email.Email);

        var confirmation = await _confirmation.SignInAsync(email.AccountId, ct);

        await _notification.SendConfirmationAsync(email.Email, confirmation.Code, request.Language, ct);

        return confirmation.Token;
    }
}
