using People.Domain.Entities;

namespace People.Api.Endpoints.Account.Models;

internal sealed record ConnectionModel(ExternalService Type, string Identity, string? FirstName, string? LastName)
{
    internal static ConnectionModel Map(ExternalConnection x) =>
        new(x.Type, x.Identity, x.FirstName, x.LastName);
}
