using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Grpc.Notification;

namespace People.Gateway.Endpoints.Messages;

[ApiController, Route("messages/email")]
public class MessagesController : ControllerBase
{
    private readonly NotificationService.NotificationServiceClient _notification;
    private readonly PeopleService.PeopleServiceClient _people;

    public MessagesController(NotificationService.NotificationServiceClient notification,
        PeopleService.PeopleServiceClient people)
    {
        _notification = notification;
        _people = people;
    }

    [HttpPost, Authorize(Policy = Policy.RequireCommonAccess)]
    public async Task<ActionResult> SendEmailAsync([FromBody] EmailMessage request, CancellationToken ct)
    {
        await _notification.SendEmailAsync(
            new SendEmailRequest
            {
                Email = request.Email,
                Body = request.Body,
                Subject = request.Subject,
                Force = true,
                UserTimeZone = TimeZoneInfo.Utc.Id
            },
            cancellationToken: ct
        );

        return Accepted();
    }

    [HttpPost("accounts/{id:long}"), Authorize(Policy = Policy.RequireCommonAccess)]
    public async Task<ActionResult> SendEmailAsync([FromRoute] long id, [FromBody] AccountEmailMessage request,
        CancellationToken ct)
    {
        var options = new CallOptions(cancellationToken: ct);

        var account = await _people.GetAccountAsync(new AccountIdValue { Value = id }, options);
        var email = account.Connections
            .First(x => x.ConnectionTypeCase == AccountReply.Types.Connection.ConnectionTypeOneofCase.Email &&
                                 x.Email.IsPrimary).Value;
        
        await _notification.SendEmailAsync(
            new SendEmailRequest
            {
                Email = email,
                Body = request.Body,
                Subject = request.Subject,
                Force = request.Force,
                UserTimeZone = account.TimeZone
            },
            options
        );

        return Accepted();
    }

    public sealed record EmailMessage(
        [Required, EmailAddress] string Email,
        [Required] string Subject,
        [Required] string Body
    );

    public sealed record AccountEmailMessage([Required] string Subject, [Required] string Body, bool Force);
}
