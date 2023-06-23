using People.Api.Application.Models;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class SignUpReply
{
    internal static SignUpReply Map(SignUpResult result) =>
        new() { Id = result.Id, FullName = result.FullName };
}
