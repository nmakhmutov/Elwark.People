using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Provider.Social.Microsoft;

public sealed class MicrosoftApiService : IMicrosoftApiService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public MicrosoftApiService(HttpClient httpClient) =>
        _httpClient = httpClient;

    public async Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync("/v1.0/users/me", ct);

        return response.StatusCode switch
        {
            HttpStatusCode.OK =>
                Success(await response.Content.ReadFromJsonAsync<Dto>(Options, ct)),

            HttpStatusCode.Unauthorized =>
                throw new PeopleException(ExceptionCodes.ProviderUnauthorized),

            _ => throw new PeopleException(ExceptionCodes.ProviderUnknown, await response.Content.ReadAsStringAsync(ct))
        };
    }

    private static MicrosoftAccount Success(Dto? account)
    {
        if (account is null)
            throw new ArgumentNullException(nameof(account), "Microsoft account cannot be null");

        return new MicrosoftAccount(
            new MicrosoftIdentity(account.Id),
            new EmailIdentity(account.UserPrincipalName),
            account.GivenName,
            account.Surname
        );
    }

    private sealed record Dto(string Id, string UserPrincipalName, string? GivenName, string? Surname);
}
