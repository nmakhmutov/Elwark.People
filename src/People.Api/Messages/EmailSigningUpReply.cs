// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class EmailSigningUpReply
{
    internal static EmailSigningUpReply Map(string token) =>
        new() { Token = token };
}
