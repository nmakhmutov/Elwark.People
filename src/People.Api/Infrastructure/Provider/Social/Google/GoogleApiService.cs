using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Provider.Social.Google
{
    public class GoogleApiService : IGoogleApiService
    {
        private readonly HttpClient _httpClient;

        public GoogleApiService(HttpClient httpClient) =>
            _httpClient = httpClient;

        public async Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("/oauth2/v1/userinfo", ct);

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

        private static GoogleAccount ParseSuccessResponse(JToken json)
        {
            var id = json.Value<string>("id") ?? throw new InvalidOperationException("Google id not found");
            var email = json.Value<string>("email") ?? throw new InvalidOperationException("Google email not found");
            
            var locale = json.Value<string?>("locale");
            var picture = json.Value<string?>("picture");
            
            return new GoogleAccount(
                new Identity.Google(id),
                new Identity.Email(email),
                json.Value<bool>("verified_email"),
                json.Value<string?>("given_name"),
                json.Value<string?>("family_name"),
                string.IsNullOrEmpty(picture) ? null : new Uri(picture),
                string.IsNullOrEmpty(locale) ? null : new CultureInfo(locale)
            );
        }
    }
}
