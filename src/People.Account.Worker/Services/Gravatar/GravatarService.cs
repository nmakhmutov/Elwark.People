using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace People.Account.Worker.Services.Gravatar
{
    public class GravatarService : IGravatarService
    {
        private readonly HttpClient _client;
        private readonly ILogger<GravatarService> _logger;

        public GravatarService(HttpClient client, ILogger<GravatarService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<GravatarProfile?> GetAsync(string email)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(email));
            var id = string.Concat(hash.Select(x => x.ToString("x2")));

            var response = await _client.GetAsync($"/{id}.json");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gravatar sent status code {Code} for hash {Id}", response.StatusCode, id);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content)
                .Property("entry")
                ?.ToArray()
                .FirstOrDefault()
                ?.First
                ?.ToObject<GravatarProfile>();
        }
    }
}