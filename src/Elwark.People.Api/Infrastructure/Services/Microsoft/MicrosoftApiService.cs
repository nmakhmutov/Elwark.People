using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Extensions;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Cache;
using Newtonsoft.Json.Linq;

namespace Elwark.People.Api.Infrastructure.Services.Microsoft
{
    public class MicrosoftApiService : IMicrosoftApiService
    {
        private readonly ICacheStorage _cache;
        private readonly HttpClient _httpClient;

        public MicrosoftApiService(HttpClient httpClient, ICacheStorage cache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = cache;
        }

        public async Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            var key = $"microsoft-api-{accessToken.ToMd5Hash()}";
            var cache = await _cache.ReadAsync<MicrosoftAccount>(key);
            if (cache is {})
                return cache;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("/v1.0/users/me", cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            if (!response.IsSuccessStatusCode)
                HandleError(response, json);

            var data = ParseSuccessResponse(json);
            await _cache.CreateAsync(key, data, TimeSpan.FromMinutes(5));

            return data;
        }

        private static void HandleError(HttpResponseMessage response, JObject json) =>
            throw (response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ElwarkMicrosoftException(MicrosoftError.TokenExpired),

                _ => new ElwarkMicrosoftException(
                    MicrosoftError.Unknown,
                    json.SelectToken("error.message")?.Value<string?>()
                )
            });

        private static MicrosoftAccount ParseSuccessResponse(JToken json) =>
            new MicrosoftAccount(
                new Identification.Microsoft(json.Value<string>("id")),
                new Identification.Email(json.Value<string>("userPrincipalName")),
                json.Value<string?>("givenName"),
                json.Value<string?>("surname")
            );
    }
}