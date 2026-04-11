using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Worker.Commands;

public sealed record SendEmailVerificationCommand(AccountId AccountId, string Email, Locale Locale) : ICommand;

public sealed class SendEmailVerificationCommandHandler : ICommandHandler<SendEmailVerificationCommand>
{
    private readonly PeopleDbContext _dbContext;
    private readonly INotificationSender _notification;

    public SendEmailVerificationCommandHandler(PeopleDbContext dbContext, INotificationSender notification)
    {
        _dbContext = dbContext;
        _notification = notification;
    }

    public async ValueTask<Unit> Handle(SendEmailVerificationCommand request, CancellationToken ct)
    {
        var email = new MailAddress(request.Email);

        var code = await _dbContext.Confirmations
            .AsNoTracking()
            .Where(x => x.AccountId == request.AccountId && x.Type == ConfirmationType.EmailConfirmation)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(ct) ?? throw ConfirmationException.NotFound();

        await _notification.SendConfirmationAsync(email, code, request.Locale, ct);

        return Unit.Value;
    }
}
