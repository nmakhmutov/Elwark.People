using People.Domain.Entities;

namespace People.Api.Application.Models;

internal sealed record SignInResult(AccountId Id, string FullName);
