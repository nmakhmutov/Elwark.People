namespace People.Application.Providers.Microsoft;

public interface IMicrosoftApiService
{
    Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
