using System.Net;
using People.Domain.ValueObjects;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class Metadata
{
    public IPAddress GetIpAddress() =>
        IPAddress.TryParse(IpAddress, out var address) ? address : IPAddress.None;

    public Timezone GetTimezone() =>
        Domain.ValueObjects.Timezone.ParseOrDefault(Timezone);
}
