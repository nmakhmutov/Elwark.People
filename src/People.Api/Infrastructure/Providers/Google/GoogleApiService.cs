using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace People.Api.Infrastructure.Providers.Google;

internal sealed class GoogleApiService : IGoogleApiService
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
        var response = await _httpClient
            .GetAsync("/oauth2/v1/userinfo", ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var google = await response.Content
            .ReadFromJsonAsync<Dto>(Options, ct)
            .ConfigureAwait(false);

        return Success(google);
    }

    private static GoogleAccount Success(Dto? account)
    {
        if (account is null)
            throw new ArgumentNullException(nameof(account), "Google account cannot be null");

        return new GoogleAccount(
            account.Id ?? throw new InvalidOperationException("Google id not found"),
            new MailAddress(account.Email ?? throw new InvalidOperationException("Google email not found")),
            account.VerifiedEmail,
            account.GivenName,
            account.FamilyName,
            string.IsNullOrWhiteSpace(account.Picture) ? null : new Uri(account.Picture),
            string.IsNullOrWhiteSpace(account.Locale) ? null : new CultureInfo(account.Locale)
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
