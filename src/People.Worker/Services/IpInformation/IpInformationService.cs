using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace People.Worker.Services.IpInformation;

public interface IIpInformationService
{
    Task<IpInformationDto?> GetAsync(string ip, string lang);
}

public sealed class IpInformationService : IIpInformationService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<IpInformationService> _logger;

    public IpInformationService(HttpClient httpClient, ILogger<IpInformationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IpInformationDto?> GetAsync(string ip, string lang)
    {
        var response = await _httpClient.GetAsync($"/json/{ip}?lang={lang}");

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<IpInformationDto>(Options);

            return data?.Status == IpInformationStatus.Success ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }
}
