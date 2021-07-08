using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;

namespace People.Gateway.Features.Timezone
{
    [ApiController, Route("timezones"), Authorize(Policy = Policy.RequireProfileAccess)]
    public class TimezonesController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _client;

        public TimezonesController(Grpc.Gateway.Gateway.GatewayClient client) =>
            _client = client;

        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            var callOptions = new CallOptions(cancellationToken: ct);
            var timezones = await _client.GetTimezonesAsync(new Empty(), callOptions);

            return Ok(timezones.Timezones.Select(x => new Timezone(x.Name, x.Offset.ToTimeSpan())));
        }
    }
}
