namespace People.Api.Infrastructure.Providers;

public interface IIpService
{
    Task<IpInformation?> GetAsync(string ip, string lang);
}
