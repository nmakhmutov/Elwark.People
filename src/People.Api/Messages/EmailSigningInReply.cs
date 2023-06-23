// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class EmailSigningInReply
{
    internal static EmailSigningInReply Map(string token) =>
        new() { Token = token };
}
