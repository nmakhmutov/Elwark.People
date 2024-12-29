using System.Text.Json.Serialization;

namespace People.Api.Infrastructure.Providers;

internal sealed class IpQueryService : IIpService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpQueryService> _logger;

    public IpQueryService(HttpClient client, IConfiguration configuration, ILogger<IpQueryService> logger)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IpInformation?> GetAsync(string ip, string lang)
    {
        var response = await _client.GetAsync($"{_configuration["Urls:IpQuery.Api"]}/{ip}?format=json");

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<IpQueryResponse>();

            if (data is null)
                return null;

            _logger.LogInformation("Received ip information {@Information}", data);

            return new IpInformation(data.Location.CountryCode, null, data.Location.City, data.Location.Timezone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }

    private sealed record IpQueryResponse(IpQueryResponse.LocationModel Location)
    {
        public sealed record LocationModel(
            [property: JsonPropertyName("country_code")] string? CountryCode,
            string? City,
            string? Timezone
        );
    }
}
