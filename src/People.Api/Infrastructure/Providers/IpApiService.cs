using System.Text.Json;
using System.Text.Json.Serialization;

namespace People.Api.Infrastructure.Providers;

public sealed class IpApiService : IIpService
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpApiService> _logger;

    public IpApiService(HttpClient client, IConfiguration configuration, ILogger<IpApiService> logger)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync(
            $"{_configuration["Urls:Ip.Api"]}/json/{ip}?lang={lang}&fields=status,continentCode,countryCode,city,timezone"
        );

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<IpApiResponse>(Options);

            if (data is null)
                return null;

            if (data.Status == Status.Fail)
                return null;

            _logger.LogInformation("Received ip information {@Information}", data);

            return new IpInformation(data.CountryCode, data.ContinentCode, data.City, data.TimeZone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }

    private sealed record IpApiResponse(
        Status Status,
        string CountryCode,
        string ContinentCode,
        string? City,
        string TimeZone
    );

    private enum Status
    {
        Success = 1,
        Fail = 2
    }
}
