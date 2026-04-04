using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Ip;

namespace People.Infrastructure.Providers.Ip;

public interface IGeoPluginService : IIpService;

internal sealed partial class GeoPluginService : IGeoPluginService
{
    private readonly HttpClient _client;
    private readonly ILogger<GeoPluginService> _logger;

    public GeoPluginService(HttpClient client, ILogger<GeoPluginService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync($"/json.gp?ip={ip}");

        if (!response.IsSuccessStatusCode)
        {
            LogReceivedError(await response.Content.ReadAsStringAsync());
            return null;
        }

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<GeoPluginResponse>();

            if (data is null)
            {
                LogReceivedError(await response.Content.ReadAsStringAsync());
                return null;
            }

            LogReceivedInformation(data);

            return new IpInformation(data.CountryCode, data.ContinentCode, data.City, data.Timezone);
        }
        catch (Exception ex)
        {
            LogReceivedException(ex);
            return null;
        }
    }

    private sealed record GeoPluginResponse(
        [property: JsonPropertyName("geoplugin_city")] string? City,
        [property: JsonPropertyName("geoplugin_countryCode")] string? CountryCode,
        [property: JsonPropertyName("geoplugin_continentCode")] string? ContinentCode,
        [property: JsonPropertyName("geoplugin_timezone")] string? Timezone
    );

    [LoggerMessage(LogLevel.Information, "Received ip information {@Information}")]
    partial void LogReceivedInformation(GeoPluginResponse information);

    [LoggerMessage(LogLevel.Warning, "Geo plugin return error response {body}")]
    partial void LogReceivedError(string body);

    [LoggerMessage(LogLevel.Warning, "Cannot deserialize geo plugin object")]
    partial void LogReceivedException(Exception exception);
}
