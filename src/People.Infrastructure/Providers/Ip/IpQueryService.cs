using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Ip;

namespace People.Infrastructure.Providers.Ip;

public interface IIpQueryService : IIpService;

internal sealed partial class IpQueryService : IIpQueryService
{
    private readonly HttpClient _client;
    private readonly ILogger<IpQueryService> _logger;

    public IpQueryService(HttpClient client, ILogger<IpQueryService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync($"/{ip}?format=json");

        if (!response.IsSuccessStatusCode)
        {
            LogReceivedError(await response.Content.ReadAsStringAsync());
            return null;
        }

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<IpQueryResponse>();

            if (data is null || data.IsEmpty)
            {
                LogReceivedError(await response.Content.ReadAsStringAsync());
                return null;
            }

            LogReceivedInformation(data);

            return new IpInformation(data.Location.CountryCode, null, data.Location.City, data.Location.Timezone);
        }
        catch (Exception ex)
        {
            LogReceivedException(ex);
            return null;
        }
    }

    [LoggerMessage(LogLevel.Information, "Received ip information {@Information}")]
    partial void LogReceivedInformation(IpQueryResponse information);

    [LoggerMessage(LogLevel.Warning, "Ip query return error response {body}")]
    partial void LogReceivedError(string body);

    [LoggerMessage(LogLevel.Warning, "Cannot deserialize ip query object")]
    partial void LogReceivedException(Exception exception);

    private sealed record IpQueryResponse(IpQueryResponse.LocationModel Location)
    {
        public bool IsEmpty =>
            string.IsNullOrEmpty(Location.CountryCode) &&
            string.IsNullOrEmpty(Location.City) &&
            string.IsNullOrEmpty(Location.Timezone);

        public sealed record LocationModel(
            [property: JsonPropertyName("country_code")] string? CountryCode,
            string? City,
            string? Timezone
        );
    }
}
