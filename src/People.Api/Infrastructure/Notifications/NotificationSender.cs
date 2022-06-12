using System.Net.Mail;
using Notification.Grpc;
using People.Api.Email.Models;
using People.Api.Infrastructure.EmailBuilder;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Infrastructure.Notifications;

internal sealed class NotificationSender : INotificationSender
{
    private readonly IEmailBuilder _emailBuilder;
    private readonly NotificationService.NotificationServiceClient _notification;

    public NotificationSender(IEmailBuilder emailBuilder, NotificationService.NotificationServiceClient notification)
    {
        _emailBuilder = emailBuilder;
        _notification = notification;
    }

    public async Task SendConfirmationAsync(MailAddress email, int code, Language language, CancellationToken ct)
    {
        var template = $"Confirmation.{language}.liquid";
        var (subject, body) = await _emailBuilder.CreateEmailAsync(template, new ConfirmationCodeModel(code));

        var message = new SendRequest { Email = email.Address, Subject = subject, Body = body };
        await _notification.SendEmailAsync(message, cancellationToken: ct);
    }
}
