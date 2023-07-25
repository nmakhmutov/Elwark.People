using System.Net;
using System.Net.Mail;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Infrastructure.Notifications;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SigningUpByEmail;

internal sealed record SigningUpByEmailCommand(MailAddress Email, Language Language, IPAddress Ip) : IRequest<string>;

internal sealed class SigningUpByEmailCommandHandler : IRequestHandler<SigningUpByEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;
    private readonly IIpHasher _hasher;
    private readonly INotificationSender _notification;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SigningUpByEmailCommandHandler(IConfirmationService confirmation, PeopleDbContext dbContext,
        IIpHasher hasher, INotificationSender notification, IAccountRepository repository, TimeProvider timeProvider)
    {
        _confirmation = confirmation;
        _dbContext = dbContext;
        _hasher = hasher;
        _notification = notification;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<string> Handle(SigningUpByEmailCommand request, CancellationToken ct)
    {
        var email = await _dbContext.Emails
            .Where(x => x.Email == request.Email.Address)
            .Select(x => new { x.AccountId, Email = new MailAddress(x.Email), x.IsConfirmed })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (email is not null)
        {
            if (email.IsConfirmed)
                throw EmailException.AlreadyCreated(request.Email);

            return await SendConfirmationAsync(email.AccountId, email.Email, request.Language, ct)
                .ConfigureAwait(false);
        }

        var account = new Account(request.Email.User, request.Language, request.Ip, _hasher);
        account.AddEmail(request.Email, false, _timeProvider);

        await _repository.AddAsync(account, ct)
            .ConfigureAwait(false);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return await SendConfirmationAsync(account.Id, account.GetPrimaryEmail(), request.Language, ct)
            .ConfigureAwait(false);
    }

    private async Task<string> SendConfirmationAsync(AccountId id, MailAddress email, Language language,
        CancellationToken ct)
    {
        var confirmation = await _confirmation.SignUpAsync(id, ct)
            .ConfigureAwait(false);

        await _notification.SendConfirmationAsync(email, confirmation.Code, language, ct)
            .ConfigureAwait(false);

        return confirmation.Token;
    }
}
