namespace People.Api.Infrastructure.Providers.Microsoft;

internal interface IMicrosoftApiService
{
    Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
