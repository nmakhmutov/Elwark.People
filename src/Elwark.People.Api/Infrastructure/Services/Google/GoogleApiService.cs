using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Api.Extensions;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Cache;
using Newtonsoft.Json.Linq;

namespace Elwark.People.Api.Infrastructure.Services.Google
{
    public class GoogleApiService : IGoogleApiService
    {
        private readonly ICacheStorage _cache;
        private readonly HttpClient _httpClient;

        public GoogleApiService(HttpClient httpClient, ICacheStorage cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<GoogleAccount> GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            var key = $"google-api-{accessToken.ToMd5Hash()}";
            var cache = await _cache.ReadAsync<GoogleAccount?>(key);
            if (cache is {})
                return cache;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("/oauth2/v1/userinfo", cancellationToken);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            if (!response.IsSuccessStatusCode)
                HandleError(response, json);

            var data = ParseSuccessResponse(json);
            await _cache.CreateAsync(key, data, TimeSpan.FromMinutes(5));

            return data;
        }

        private static void HandleError(HttpResponseMessage response, JToken json) =>
            throw (response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                new ElwarkGoogleException(GoogleError.TokenExpired),

                _ =>
                new ElwarkGoogleException(GoogleError.Unknown, GetErrorMessage(json))
            });

        private static GoogleAccount ParseSuccessResponse(JToken json)
        {
            var locale = json.Value<string?>("locale").NullIfEmpty();

            return new GoogleAccount(
                new Identification.Google(json.Value<string>("id")),
                new Identification.Email(json.Value<string>("email")),
                json.Value<bool>("verified_email"),
                json.Value<string?>("given_name"),
                json.Value<string?>("family_name"),
                json.Value<string?>("picture").ToUri(),
                locale is null ? null : new CultureInfo(locale)
            );
        }

        private static string? GetErrorMessage(JToken json) =>
            json.SelectToken("error.message")?.Value<string?>();
    }
}