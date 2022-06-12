using System.Net.Mail;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Infrastructure.Notifications;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SigningInByEmail;

internal sealed record SigningInByEmailCommand(MailAddress Email, Language Language) : IRequest<string>;

internal sealed class SigningInByEmailCommandHandler : IRequestHandler<SigningInByEmailCommand, string>
{
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;
    private readonly INotificationSender _notification;
    private readonly ITimeProvider _time;

    public SigningInByEmailCommandHandler(IConfirmationService confirmation, PeopleDbContext dbContext,
        INotificationSender notification, ITimeProvider time)
    {
        _confirmation = confirmation;
        _dbContext = dbContext;
        _notification = notification;
        _time = time;
    }

    public async Task<string> Handle(SigningInByEmailCommand request, CancellationToken ct)
    {
        var email = await _dbContext.Emails
            .Where(x => x.Email == request.Email.Address)
            .Select(x => new { x.AccountId, Email = new MailAddress(x.Email), x.IsConfirmed })
            .FirstOrDefaultAsync(ct) ?? throw EmailException.NotFound(request.Email);

        if (!email.IsConfirmed)
            throw EmailException.NotConfirmed(email.Email);

        var confirmation = await _confirmation.CreateSignInAsync(email.AccountId, _time);
        await _notification.SendConfirmationAsync(email.Email, confirmation.Code, request.Language, ct);

        return confirmation.Token;
    }
}
