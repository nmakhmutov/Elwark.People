using System.Net;
using System.Net.Mail;
using Mediator;
using People.Api.Infrastructure.Notifications;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SigningUpByEmail;

internal sealed record SigningUpByEmailCommand(MailAddress Email, Language Language, IPAddress Ip, string? UserAgent) :
    IRequest<string>;

internal sealed class SigningUpByEmailCommandHandler : IRequestHandler<SigningUpByEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly IIpHasher _hasher;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SigningUpByEmailCommandHandler(
        IConfirmationService confirmation,
        IIpHasher hasher,
        INotificationSender notification,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _hasher = hasher;
        _notification = notification;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<string> Handle(SigningUpByEmailCommand request, CancellationToken ct)
    {
        var email = await _repository.GetEmailSignupStateAsync(request.Email, ct);

        if (email is not null)
        {
            if (email.IsConfirmed)
                throw EmailException.AlreadyCreated(request.Email);

            return await SendConfirmationAsync(email.AccountId, email.Email, request.Language, ct);
        }

        var account = Account.Create(request.Email.User, request.Language, request.Ip, _hasher, _timeProvider);
        account.AddEmail(request.Email, false, _timeProvider);

        await _repository.AddAsync(account, ct);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return await SendConfirmationAsync(account.Id, account.GetPrimaryEmail(), request.Language, ct);
    }

    private async Task<string> SendConfirmationAsync(
        AccountId id,
        MailAddress email,
        Language language,
        CancellationToken ct
    )
    {
        var confirmation = await _confirmation.SignUpAsync(id, ct);

        await _notification.SendConfirmationAsync(email, confirmation.Code, language, ct);

        return confirmation.Token;
    }
}
