using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Models;

namespace People.Gateway.Controllers
{
    [ApiController, Route("infrastructure"), Authorize(Policy = Policy.RequireProfileAccess)]
    public sealed class InfrastructureController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _client;

        public InfrastructureController(Grpc.Gateway.Gateway.GatewayClient client) =>
            _client = client;
        
        [HttpGet]
        public async Task<ActionResult> GetListsAsync(CancellationToken ct)
        {
            var callOptions = new CallOptions(cancellationToken: ct);
            var countriesTask = _client.GetCountriesAsync(new Empty(), callOptions)
                .ResponseAsync;

            var timezonesTask = _client.GetTimezonesAsync(new Empty(), callOptions)
                .ResponseAsync;

            await Task.WhenAll(countriesTask, timezonesTask);

            return Ok(
                new Lists(
                    countriesTask.Result.Countries.Select(x => new Country(x.Code, x.Name)),
                    timezonesTask.Result.Timezones.Select(x => new Timezone(x.Name, x.Offset.ToTimeSpan()))
                )
            );
        }
    }
}