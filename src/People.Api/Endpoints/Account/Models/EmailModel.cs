using People.Api.Application.Queries.GetEmails;
using People.Domain.Entities;

namespace People.Api.Endpoints.Account.Models;

internal sealed record EmailModel(string Value, bool IsPrimary, bool IsConfirmed)
{
    internal static EmailModel Map(EmailAccount x) =>
        new(x.Email, x.IsPrimary, x.IsConfirmed);

    internal static EmailModel Map(UserEmail x) =>
        new(x.Email, x.IsPrimary, x.IsConfirmed);
}
