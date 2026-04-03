using System.Globalization;
using System.Net;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class Metadata
{
    public IPAddress GetIpAddress() =>
        IPAddress.TryParse(IpAddress, out var address) ? address : IPAddress.None;

    public CultureInfo GetLocale()
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
}
