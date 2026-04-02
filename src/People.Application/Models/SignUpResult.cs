using People.Domain.Entities;

namespace People.Application.Models;

public sealed record SignUpResult(AccountId Id, string FullName);
