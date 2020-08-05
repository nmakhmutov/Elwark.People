namespace Elwark.People.Background.Models
{
    public class IpInformationDto
    {
        public IpInformationDto(IpInformationStatus status, string timezone, string? city, string? country,
            string countryCode, string? isp, double? lat, double? lon, string? org, string? query, string? region,
            string? regionName)
        {
            City = city;
            Country = country;
            CountryCode = countryCode;
            Isp = isp;
            Lat = lat;
            Lon = lon;
            Org = org;
            Query = query;
            Region = region;
            RegionName = regionName;
            Status = status;
            Timezone = timezone;
        }

        public string? City { get; set; }
        public string? Country { get; set; }
        public string CountryCode { get; set; }
        public string? Isp { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public string? Org { get; set; }
        public string? Query { get; set; }
        public string? Region { get; set; }
        public string? RegionName { get; set; }
        public IpInformationStatus Status { get; set; }
        public string Timezone { get; set; }
    }

    public enum IpInformationStatus
    {
        Success = 1,
        Fail = 2
    }
}