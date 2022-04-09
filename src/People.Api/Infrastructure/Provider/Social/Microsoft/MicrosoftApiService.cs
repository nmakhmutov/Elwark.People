using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Provider.Social.Microsoft;

public sealed class MicrosoftApiService : IMicrosoftApiService
{
    private readonly HttpClient _httpClient;

    public MicrosoftApiService(HttpClient httpClient) =>
        _httpClient = httpClient;

    public async Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync("/v1.0/users/me", ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        var json = JObject.Parse(content);

        return response.StatusCode switch
        {
            HttpStatusCode.OK =>
                ParseSuccessResponse(json),

            HttpStatusCode.Unauthorized =>
                throw new PeopleException(ExceptionCodes.ProviderUnauthorized),

            _ => throw new PeopleException(ExceptionCodes.ProviderUnknown,
                json.SelectToken("error.message")?.Value<string?>())
        };
    }

    private static MicrosoftAccount ParseSuccessResponse(JToken json)
    {
        var id = json.Value<string>("id") ?? throw new InvalidOperationException("Microsoft id not found");
        var email = json.Value<string>("userPrincipalName") ??
                    throw new InvalidOperationException("Microsoft email not found");

        return new MicrosoftAccount(
            new MicrosoftIdentity(id),
            new EmailIdentity(email),
            json.Value<string>("givenName"),
            json.Value<string>("surname")
        );
    }
}
