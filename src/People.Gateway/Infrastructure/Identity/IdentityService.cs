using System;
using Microsoft.AspNetCore.Http;
using People.Grpc.Common;

namespace People.Gateway.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public long GetId()
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirst(x => x.Type == "sub");
        if (sub is not null)
            return long.Parse(sub.Value);

        throw new ArgumentException("Account id cannot be null in identity service");
    }

    public AccountIdValue GetAccountId() =>
        new() { Value = GetId() };
}
