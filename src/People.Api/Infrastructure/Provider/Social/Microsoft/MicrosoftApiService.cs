using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Provider.Social.Microsoft
{
    public class MicrosoftApiService : IMicrosoftApiService
    {
        private readonly HttpClient _httpClient;

        public MicrosoftApiService(HttpClient httpClient) =>
            _httpClient = httpClient;

        public async Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("/v1.0/users/me", ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            var json = JObject.Parse(content);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => new MicrosoftAccount(
                    new MicrosoftIdentity(json.Value<string>("id")),
                    new EmailIdentity(json.Value<string>("userPrincipalName")),
                    json.Value<string?>("givenName"),
                    json.Value<string?>("surname")
                ),

                HttpStatusCode.Unauthorized =>
                    throw new ElwarkException(ElwarkExceptionCodes.ProviderUnauthorized),

                _ => throw new ElwarkException(ElwarkExceptionCodes.ProviderUnknown,
                    json.SelectToken("error.message")?.Value<string?>())
            };
        }
    }
}