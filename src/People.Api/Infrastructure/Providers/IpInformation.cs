namespace People.Api.Infrastructure.Providers;

public sealed record IpInformation(string? CountryCode, string? Region, string? City, string? TimeZone);
