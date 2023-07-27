namespace People.Api.Infrastructure.Providers.IpApi;

public sealed record IpApiDto(
    Status Status,
    string ContinentCode,
    string CountryCode,
    string Region,
    string? City,
    string TimeZone
);

public enum Status
{
    Success = 1,
    Fail = 2
}
