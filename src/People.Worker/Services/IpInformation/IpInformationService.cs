using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace People.Worker.Services.IpInformation
{
    public interface IIpInformationService
    {
        Task<IpInformationDto?> GetIpInformationAsync(IPAddress ip, string lang);
    }

    public class IpInformationService : IIpInformationService
    {
        private readonly HttpClient _httpClient;

        public IpInformationService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<IpInformationDto?> GetIpInformationAsync(IPAddress ip, string lang)
        {
            var response = await _httpClient.GetAsync($"/json/{ip}?lang={lang}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<IpInformationDto>();
                if (data.Status == IpInformationStatus.Success)
                    return data;
            }

            return null;
        }
    }
}