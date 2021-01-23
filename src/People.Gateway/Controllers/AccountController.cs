using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Gateway.Models;
using People.Grpc.Common;
using Address = People.Gateway.Models.Address;
using Timezone = People.Gateway.Models.Timezone;

namespace People.Gateway.Controllers
{
    [ApiController, Route("account")]
    public class AccountController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _client;
        private readonly IIdentityService _identity;

        public AccountController(Grpc.Gateway.Gateway.GatewayClient client, IIdentityService identity)
        {
            _client = client;
            _identity = identity;
        }

        [Route("me"), Authorize(Policy = Policy.RequireAccountId)]
        public async Task<Account> MeAsync(CancellationToken ct)
        {
            var id = _identity.GetAccountId();
            var account = await _client.GetAccountAsync(new AccountId {Value = id}, cancellationToken: ct);

            return new Account(
                account.Id.Value,
                account.Name.Nickname,
                account.Name.FirstName,
                account.Name.LastName,
                account.Name.FullName,
                account.Profile.Language,
                account.Profile.Gender,
                account.Profile.Birthday?.ToDateTime(),
                account.Profile.Bio,
                account.Profile.Picture,
                new Email(account.Email.Email, account.Email.IsConfirmed),
                new Address(account.Address.CountryCode, account.Address.CityName),
                new Timezone(account.Timezone.Name, account.Timezone.Offset.ToTimeSpan()),
                account.IsBanned
            );
        }
    }
}