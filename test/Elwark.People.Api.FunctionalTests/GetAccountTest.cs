using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elwark.People.Api.Application.Models;
using Elwark.People.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Elwark.People.Api.FunctionalTests
{
    public class GetAccountTest : ScenarioBase
    {
        private static string GetAccount(long id) => $"accounts/{id}";

        [Fact]
        public async Task Get_account_by_id_ok_response()
        {
            const long id = 1;
            using var server = CreateServer();
            var json = await server.CreateClient()
                .GetStringAsync(GetAccount(id));

            await using var context = server.Services.GetService<OAuthContext>();
            var accountDb = context.Accounts
                .First(x => x.Id == id);

            var accountResponse = JsonConvert.DeserializeObject<AccountModel>(json);

            Assert.Equal(id, accountResponse.Id);
            Assert.Equal(accountDb.Name.FirstName, accountResponse.FirstName);
            Assert.Equal(accountDb.BasicInfo.Gender, accountResponse.Gender);
        }

        [Fact]
        public async Task Get_account_not_found_response()
        {
            const long id = long.MaxValue;
            using var server = CreateServer();
            var data = await server.CreateClient()
                .GetAsync(GetAccount(id));

            Assert.Equal(HttpStatusCode.NotFound, data.StatusCode);
        }

        [Fact]
        public async Task Get_account_by_incorrect_id_not_found_response()
        {
            const long id = long.MinValue;
            using var server = CreateServer();
            var data = await server.CreateClient()
                .GetAsync(GetAccount(id));

            Assert.Equal(HttpStatusCode.NotFound, data.StatusCode);
        }
    }
}