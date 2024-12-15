using People.Api.Application.Models;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class SignInReply
{
    internal static SignInReply Map(SignInResult result) =>
        new()
        {
            Id = result.Id,
            FullName = result.FullName
        };
}
