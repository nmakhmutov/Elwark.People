using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace People.Worker.Services.IpInformation;

public interface IIpInformationService
{
    Task<IpInformationDto?> GetAsync(string ip, string lang);
}

public sealed class IpInformationService : IIpInformationService
{
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

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            var data = JsonConvert.DeserializeObject<IpInformationDto>(json);

            return data is not null && data.Status == IpInformationStatus.Success ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize ip information object");
            return null;
        }
    }
}
