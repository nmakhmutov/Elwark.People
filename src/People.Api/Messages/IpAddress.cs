using System.Net;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class IpAddress
{
    public IPAddress ToIpAddress() =>
        IPAddress.TryParse(Value, out var address) ? address : IPAddress.None;
}
