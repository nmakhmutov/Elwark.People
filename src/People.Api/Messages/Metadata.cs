using System.Globalization;
using System.Net;
using People.Domain.ValueObjects;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class Metadata
{
    public IPAddress GetIpAddress() =>
        IPAddress.TryParse(IpAddress, out var address) ? address : IPAddress.None;

    public CultureInfo GetCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo(Locale);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public Timezone GetTimezone() =>
        Domain.ValueObjects.Timezone.ParseOrDefault(Timezone);
}
