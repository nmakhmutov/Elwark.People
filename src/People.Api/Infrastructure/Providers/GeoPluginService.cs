using System.Text.Json.Serialization;

namespace People.Api.Infrastructure.Providers;

public sealed class GeoPluginService : IIpService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeoPluginService> _logger;

    public GeoPluginService(HttpClient client, IConfiguration configuration, ILogger<GeoPluginService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync($"{_configuration["Urls:GeoPlugin.Api"]}/json.gp?ip={ip}");

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<GeoPluginResponse>();

            if (data is null)
                return null;

            _logger.LogInformation("Received ip information {@Information}", data);

            return new IpInformation(data.CountryCode, data.ContinentCode, data.City, data.Timezone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }

    private sealed record GeoPluginResponse(
        [property: JsonPropertyName("geoplugin_city")] string? City,
        [property: JsonPropertyName("geoplugin_countryCode")] string? CountryCode,
        [property: JsonPropertyName("geoplugin_continentCode")] string? ContinentCode,
        [property: JsonPropertyName("geoplugin_timezone")] string? Timezone
    );
}

