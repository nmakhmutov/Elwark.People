using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;

namespace People.Api.Infrastructure.Providers.Microsoft;

internal sealed class MicrosoftApiService : IMicrosoftApiService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public MicrosoftApiService(HttpClient client) =>
        _client = client;

    public async Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync("/v1.0/users/me", ct);

        response.EnsureSuccessStatusCode();
        var microsoft = await response.Content
            .ReadFromJsonAsync<Dto>(Options, ct);

        return Success(microsoft);
    }

    private static MicrosoftAccount Success(Dto? account)
    {
        if (account is null)
            throw new ArgumentNullException(nameof(account), "Microsoft account cannot be null");

        return new MicrosoftAccount(
            account.Id,
            new MailAddress(account.UserPrincipalName),
            account.GivenName,
            account.Surname
        );
    }

    private sealed record Dto(string Id, string UserPrincipalName, string? GivenName, string? Surname);
}
