using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Endpoints.Account.Models;

internal sealed record ConnectionModel(ExternalService Type, string Identity, string? FirstName, string? LastName);
