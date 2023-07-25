using People.Domain.Entities;

namespace People.Api.Application.Models;

internal sealed record SignUpResult(AccountId Id, string FullName);
