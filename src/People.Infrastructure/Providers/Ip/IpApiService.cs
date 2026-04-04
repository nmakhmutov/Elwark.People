using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Ip;

namespace People.Infrastructure.Providers.Ip;

public interface IIpApiService : IIpService;

internal sealed partial class IpApiService : IIpApiService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly HttpClient _client;
    private readonly ILogger<IpApiService> _logger;

    public IpApiService(HttpClient client, ILogger<IpApiService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync($"/json/{ip}?lang={lang}&fields=status,continentCode,countryCode,city,timezone");

        if (!response.IsSuccessStatusCode)
        {
            LogReceivedError(await response.Content.ReadAsStringAsync());
            return null;
        }

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<IpApiResponse>(Options);

            if (data is null || data.Status.Equals("Fail", StringComparison.OrdinalIgnoreCase))
            {
                LogReceivedError(await response.Content.ReadAsStringAsync());
                return null;
            }

            LogReceivedInformation(data);

            return new IpInformation(data.CountryCode, data.ContinentCode, data.City, data.TimeZone);
        }
        catch (Exception ex)
        {
            LogReceivedException(ex);
            return null;
        }
    }

    private sealed record IpApiResponse(
        string Status,
        string CountryCode,
        string ContinentCode,
        string? City,
        string TimeZone
    );

    [LoggerMessage(LogLevel.Information, "Received ip information {@Information}")]
    partial void LogReceivedInformation(IpApiResponse information);

    [LoggerMessage(LogLevel.Warning, "Ip api return error response {body}")]
    partial void LogReceivedError(string body);

    [LoggerMessage(LogLevel.Warning, "Cannot deserialize ip api object")]
    partial void LogReceivedException(Exception exception);
}
