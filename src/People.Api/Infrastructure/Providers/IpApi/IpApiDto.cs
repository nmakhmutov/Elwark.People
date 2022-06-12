namespace People.Api.Infrastructure.Providers.IpApi;

public sealed record IpApiDto(IpInformationStatus Status, string TimeZone, double? Lat, double? Lon, string? City,
    string? Country, string CountryCode, string? Isp, string? Org, string? Query, string? Region, string? RegionName);

public enum IpInformationStatus
{
    Success = 1,
    Fail = 2
}
