using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace People.Worker.Services.IpInformation;

public interface IIpInformationService
{
    Task<IpInformationDto?> GetAsync(string ip, string lang);
}

public class IpInformationService : IIpInformationService
{
    private readonly HttpClient _httpClient;

    public IpInformationService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IpInformationDto?> GetAsync(string ip, string lang)
    {
        var response = await _httpClient.GetAsync($"/json/{ip}?lang={lang}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json))
            return null;

        var data = JsonConvert.DeserializeObject<IpInformationDto>(json);

        return data is not null && data.Status == IpInformationStatus.Success ? data : null;
    }
}
