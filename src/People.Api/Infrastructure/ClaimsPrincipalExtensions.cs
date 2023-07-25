using System.Security.Claims;
using People.Domain.Entities;

namespace People.Api.Infrastructure;

internal static class ClaimsPrincipalExtensions
{
    internal static AccountId GetAccountId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub))
            throw new ArgumentException("Account id cannot be null in identity service");

        return long.Parse(sub);
    }
}
