namespace People.Api.Endpoints.Account.Models;

internal sealed record EmailModel(string Value, bool IsPrimary, bool IsConfirmed);
