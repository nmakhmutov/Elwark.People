namespace People.Application.Providers.Google;

public interface IGoogleApiService
{
    Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
