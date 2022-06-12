namespace People.Api.Infrastructure.Providers.Google;

internal interface IGoogleApiService
{
    Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
