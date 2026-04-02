namespace People.Application.Providers.Ip;

public interface IIpService
{
    Task<IpInformation?> GetAsync(string ip, string lang);
}
