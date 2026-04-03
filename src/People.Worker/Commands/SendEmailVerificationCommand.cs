using System.Net.Mail;
using System.Security.Cryptography;
using Mediator;
using People.Application.Providers;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Worker.Commands;

public sealed record SendEmailVerificationCommand(
    long AccountId,
    Guid ConfirmationId,
    string Email,
    string Language,
    DateTime OccurredAt
) : ICommand;

public sealed class SendEmailVerificationCommandHandler : ICommandHandler<SendEmailVerificationCommand>
{
    private const string ConfirmationType = "EmailVerify";
    private const string CodeChars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(30);

    private readonly PeopleDbContext _dbContext;
    private readonly INotificationSender _notification;

    public SendEmailVerificationCommandHandler(PeopleDbContext dbContext, INotificationSender notification)
    {
        _dbContext = dbContext;
        _notification = notification;
    }

    public async ValueTask<Unit> Handle(SendEmailVerificationCommand request, CancellationToken ct)
    {
        var code = RandomNumberGenerator.GetString(CodeChars, 6);
        var email = new MailAddress(request.Email);
        var accountId = new AccountId(request.AccountId);
        var language = Language.Parse(request.Language);

        var confirmation = new Confirmation(
            request.ConfirmationId,
            accountId,
            code,
            ConfirmationType,
            request.OccurredAt,
            CodeTtl
        );

        await _dbContext.Confirmations.AddAsync(confirmation, ct);
        await _notification.SendConfirmationAsync(email, code, language, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
