using System.Text.Json;
using System.Text.Json.Serialization;

namespace People.Api.Infrastructure.Providers.IpApi;

public sealed class IpApiService : IIpApiService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _client;
    private readonly ILogger<IpApiService> _logger;

    public IpApiService(HttpClient client, ILogger<IpApiService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IpApiDto?> GetAsync(string ip, string lang)
    {
        var uri = $"/json/{ip}?lang={lang}&fields=status,continentCode,countryCode,region,city,timezone";

        var response = await _client.GetAsync(uri)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<IpApiDto>(Options)
                .ConfigureAwait(false);

            return data?.Status == Status.Success ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }
}
