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

namespace Elwark.People.Api.Infrastructure.Services.Facebook
{
    public class FacebookApiService : IFacebookApiService
    {
        private readonly ICacheStorage _cache;
        private readonly string _fields;
        private readonly HttpClient _httpClient;

        public FacebookApiService(HttpClient httpClient, ICacheStorage cache)
        {
            _httpClient = httpClient;
            _cache = cache;
            _fields = string.Join(",", "email", "id", "gender", "birthday", "first_name", "last_name", "link",
                "picture.type(large){url}");
        }

        public async Task<FacebookAccount> GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            var key = $"facebook-api-{accessToken.ToMd5Hash()}";
            var cache = await _cache.ReadAsync<FacebookAccount>(key);
            if (cache is {})
                return cache;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"/v6.0/me?fields={_fields}", cancellationToken);

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
                HttpStatusCode.Unauthorized =>
                new ElwarkFacebookException(FacebookError.TokenExpired),

                _ =>
                new ElwarkFacebookException(FacebookError.Unknown, GetErrorMessage(json))
            });

        private static FacebookAccount ParseSuccessResponse(JToken json) =>
            new FacebookAccount(
                new Identification.Facebook(json.Value<string>("id")),
                new Identification.Email(json.Value<string>("email")),
                Enum.TryParse<Gender>(json.Value<string?>("gender"), true, out var gender) ? gender : (Gender?) null,
                DateTime.TryParseExact(json.Value<string?>("birthday"), "MM/dd/yyyy", null,
                    DateTimeStyles.AssumeUniversal, out var birthday)
                    ? birthday
                    : (DateTime?) null,
                json.Value<string?>("first_name"),
                json.Value<string?>("last_name"),
                new Uri(json.Value<string>("link")),
                json.SelectToken("picture.data")?.Value<string?>("url").ToUri()
            );

        private static string? GetErrorMessage(JToken json) =>
            json.SelectToken("error.message")?.Value<string?>();
    }
}