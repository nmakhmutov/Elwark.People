using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Elwark.People.Background.Models;

namespace Elwark.People.Background.Services
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
                var data = await response.Content.ReadAsAsync<IpInformationDto>();
                if (data.Status == IpInformationStatus.Success)
                    return data;
            }

            return null;
        }
    }
}