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

    private readonly HttpClient _httpClient;
    private readonly ILogger<IpApiService> _logger;

    public IpApiService(HttpClient httpClient, ILogger<IpApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IpApiDto?> GetAsync(string ip, string lang)
    {
        var response = await _httpClient.GetAsync($"/json/{ip}?lang={lang}");

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<IpApiDto>(Options);

            return data?.Status == IpInformationStatus.Success ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }
}
