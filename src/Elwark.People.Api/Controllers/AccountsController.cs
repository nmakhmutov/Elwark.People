using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Commands.AttachIdentity;
using Elwark.People.Api.Application.Commands.SignUp;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountsController(IMediator mediator) =>
            _mediator = mediator;

        [HttpGet("{id}"), Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<AccountModel>> GetAccountAsync(AccountId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAccountByIdQuery(id), ct)
                         ?? throw ElwarkAccountException.NotFound(id);

            return Ok(result);
        }

        [HttpGet("{id}/email"), Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<IEnumerable<EmailModel>>> GetEmailsAsync(AccountId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEmailsByAccountIdQuery(id), ct);

            return Ok(result);
        }

        [HttpGet("{id}/ban"), Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<BanModel>> GetBanAsync(AccountId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetBanQuery(id), ct);

            return Ok(result);
        }

        [HttpPost("email"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByEmailRequest request,
            CancellationToken ct)
        {
            var query = new SignUpByEmailCommand(request.Email, request.Password);
            var result = await _mediator.Send(query, ct);

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("google"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByGoogleRequest request,
            CancellationToken ct)
        {
            var query = new SignUpByGoogleCommand(request.Google, request.Email, request.AccessToken);
            var result = await _mediator.Send(query, ct);

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("facebook"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByFacebookRequest request,
            CancellationToken ct)
        {
            var query = new SignUpByFacebookCommand(request.Facebook, request.Email, request.AccessToken);
            var result = await _mediator.Send(query, ct);

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("microsoft"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByMicrosoftRequest request,
            CancellationToken ct)
        {
            var query = new SignUpByMicrosoftCommand(request.Microsoft, request.Email, request.AccessToken);
            var result = await _mediator.Send(query, ct);

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }


        [HttpPost("{id}/google"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachGoogleAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachGoogleIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("{id}/facebook"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachFacebookAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachFacebookIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("{id}/microsoft"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachMicrosoftAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachMicrosoftIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("confirm/resend"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> PostAsync([FromBody] SendConfirmationRequest request, CancellationToken ct)
        {
            var identity = await _mediator.Send(new GetIdentityByIdentifierQuery(request.Email), ct)
                           ?? throw ElwarkIdentificationException.NotFound(request.Email);

            await _mediator.Send(
                new SendConfirmationUrlCommand(
                    identity.AccountId,
                    identity.IdentityId,
                    new Notification.PrimaryEmail(request.Email.Value),
                    ConfirmationType.ConfirmIdentity,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture),
                ct
            );

            return NoContent();
        }

        [HttpPost("activate"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult<IdentityId>> PutAsync([FromBody] DecodeConfirmationQuery query,
            CancellationToken ct)
        {
            var decode = await _mediator.Send(query, ct);
            var confirmation = await _mediator.Send(
                new CheckConfirmationQuery(decode.IdentityId, decode.Type, decode.Code), ct
            );
            await _mediator.Send(new ActivateIdentityCommand(confirmation.IdentityId), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.IdentityId, confirmation.Type), ct);

            return Ok(confirmation.IdentityId);
        }

        private async Task SendNotificationAsync(SignUpModel model, UrlTemplate template, CancellationToken ct)
        {
            foreach (var identity in model.Identities)
            {
                if (identity.ConfirmedAt.HasValue)
                    continue;

                Notification notification = identity.Notification switch
                {
                    Notification.PrimaryEmail email => email,
                    _ => await _mediator.Send(new GetNotifierQuery(model.AccountId, NotificationType.PrimaryEmail), ct)
                } ?? throw new ElwarkNotificationException(NotificationError.NotFound);

                await _mediator.Send(
                    new SendConfirmationUrlCommand(
                        model.AccountId,
                        identity.IdentityId,
                        notification,
                        ConfirmationType.ConfirmIdentity,
                        template,
                        CultureInfo.CurrentCulture
                    ),
                    ct
                );
            }
        }
    }
}