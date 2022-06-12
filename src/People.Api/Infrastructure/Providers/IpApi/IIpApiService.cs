namespace People.Api.Infrastructure.Providers.IpApi;

public interface IIpApiService
{
    Task<IpApiDto?> GetAsync(string ip, string lang);
}
