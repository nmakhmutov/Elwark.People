using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace People.Api.Infrastructure.Provider.Social.Google;

public sealed class GoogleApiService : IGoogleApiService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GoogleApiService(HttpClient httpClient) =>
        _httpClient = httpClient;

    public async Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync("/oauth2/v1/userinfo", ct);

        return response.StatusCode switch
        {
            HttpStatusCode.OK =>
                Success(await response.Content.ReadFromJsonAsync<Dto>(Options, ct)),

            HttpStatusCode.Unauthorized =>
                throw new PeopleException(ExceptionCodes.ProviderUnauthorized),

            _ => throw new PeopleException(ExceptionCodes.ProviderUnknown, await response.Content.ReadAsStringAsync(ct))
        };
    }

    private static GoogleAccount Success(Dto? account)
    {
        if (account is null)
            throw new ArgumentNullException(nameof(account), "Google account cannot be null");

        return new GoogleAccount(
            new GoogleIdentity(account.Id ?? throw new InvalidOperationException("Google id not found")),
            new EmailIdentity(account.Email ?? throw new InvalidOperationException("Google email not found")),
            account.VerifiedEmail,
            account.GivenName,
            account.FamilyName,
            string.IsNullOrEmpty(account.Picture) ? null : new Uri(account.Picture),
            string.IsNullOrEmpty(account.Locale) ? null : new CultureInfo(account.Locale)
        );
    }

    private record Dto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("locale")]
        public string? Locale { get; init; }

        [JsonPropertyName("picture")]
        public string? Picture { get; init; }

        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }
    }
}
