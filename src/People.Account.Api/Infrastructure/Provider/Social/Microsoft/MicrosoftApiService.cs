using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Infrastructure.Provider.Social.Microsoft
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
                HttpStatusCode.OK =>
                    ParseSuccessResponse(json),

                HttpStatusCode.Unauthorized =>
                    throw new ElwarkException(ElwarkExceptionCodes.ProviderUnauthorized),

                _ => throw new ElwarkException(ElwarkExceptionCodes.ProviderUnknown,
                    json.SelectToken("error.message")?.Value<string?>())
            };
        }

        private static MicrosoftAccount ParseSuccessResponse(JToken json)
        {
            var id = json.Value<string>("id") ?? throw new InvalidOperationException("Microsoft id not found");
            var email = json.Value<string>("userPrincipalName") ??
                        throw new InvalidOperationException("Microsoft email not found");

            return new MicrosoftAccount(
                new Identity.Microsoft(id),
                new Identity.Email(email),
                json.Value<string>("givenName"),
                json.Value<string>("surname")
            );
        }
    }
}
