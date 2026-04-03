using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Integration.Api.Tests.Web;

internal static class JwtTestTokens
{
    internal const string Issuer = "https://people.api.tests";
    internal const string Audience = "people-api";

    internal static readonly SymmetricSecurityKey SigningKey = new("people-api-integration-test-secret-key-32b!!"u8.ToArray());

    /// <summary>JWT with <c>sub</c> only (no <c>scope</c> claim).</summary>
    internal static string CreateBearerWithoutScope(long accountId)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, accountId.ToString()) };
        return CreateBearerCore(claims);
    }

    internal static string CreateBearer(long accountId, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new("scope", string.Join(' ', scopes))
        };

        return CreateBearerCore(claims);
    }

    internal static string CreateBearerWithRole(long accountId, string role, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new(ClaimTypes.Role, role),
            new("scope", string.Join(' ', scopes))
        };

        return CreateBearerCore(claims);
    }

    private static string CreateBearerCore(List<Claim> claims)
    {
        var creds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
