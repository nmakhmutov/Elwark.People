using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace People.Api.Infrastructure.Providers.Gravatar;

public sealed class GravatarService : IGravatarService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _client;
    private readonly ILogger<GravatarService> _logger;

    public GravatarService(HttpClient client, ILogger<GravatarService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<GravatarProfile?> GetAsync(MailAddress email)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(email.Address));
        var id = string.Concat(hash.Select(x => x.ToString("x2")));

        var response = await _client
            .GetAsync($"/{id}.json")
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Gravatar sent status code {Code} for hash {Id}", response.StatusCode, id);
            return null;
        }

        try
        {
            var content = await response.Content
                .ReadFromJsonAsync<Wrapper>(Options)
                .ConfigureAwait(false);
            
            return content?.Entry.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot deserialize gravatar object for {id}", id);
            return null;
        }
    }

    private sealed record Wrapper(GravatarProfile[] Entry);
}
