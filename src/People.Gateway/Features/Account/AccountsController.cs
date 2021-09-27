using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Gateway.Mappes;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Grpc.Notification;

namespace People.Gateway.Features.Account
{
    [ApiController, Route("accounts")]
    public sealed class AccountsController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _gateway;
        private readonly NotificationService.NotificationServiceClient _notification;
        private readonly IIdentityService _identity;

        public AccountsController(Grpc.Gateway.Gateway.GatewayClient gateway, IIdentityService identity,
            NotificationService.NotificationServiceClient notification)
        {
            _gateway = gateway;
            _identity = identity;
            _notification = notification;
        }

        [HttpGet("me"), Authorize(Policy = Policy.RequireAccountId)]
        public async Task<ActionResult> GetAsync(CancellationToken ct)
        {
            var account = await _gateway.GetProfileAsync(_identity.GetAccountId(), cancellationToken: ct);

            return Ok(ToAccount(account));
        }

        [HttpGet("{id:long}"), Authorize(Policy = Policy.RequireCommonAccess)]
        public async Task<ActionResult> GetAsync(long id, CancellationToken ct)
        {
            var account = await _gateway.GetProfileAsync(new AccountId { Value = id }, cancellationToken: ct);

            return Ok(ToAccount(account));
        }

        [HttpPost("{id:long}/email"), Authorize(Policy = Policy.RequireCommonAccess)]
        public async Task<ActionResult> SendEmailAsync([FromRoute] long id, [FromBody] SendEmailRequest request,
            CancellationToken ct)
        {
            var options = new CallOptions(cancellationToken: ct);

            var information = await _gateway.GetEmailNotificationAsync(new AccountId { Value = id }, options);

            await _notification.SendEmailAsync(new Grpc.Notification.SendEmailRequest
                {
                    Email = information.PrimaryEmail,
                    Body = request.Body,
                    Subject = request.Subject,
                    IsNow = request.IsNow,
                    UserTimeZone = information.TimeZone
                },
                options
            );

            return Accepted();
        }

        private static Account ToAccount(ProfileReply account) =>
            new(
                account.Id.Value,
                account.Name.Nickname,
                account.Name.FirstName,
                account.Name.LastName,
                account.Name.FullName,
                account.Language,
                account.Picture,
                account.CountryCode,
                account.TimeZone,
                account.FirstDayOfWeek.FromGrpc(),
                account.Ban is not null
            );

        public sealed record SendEmailRequest([Required] string Subject, [Required] string Body, bool IsNow);
    }
}
