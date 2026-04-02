using People.Domain.Entities;

namespace People.Application.Models;

public sealed record SignInResult(AccountId Id, string FullName);
