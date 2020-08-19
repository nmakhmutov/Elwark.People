using System;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Elwark.People.Api.Infrastructure.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityService(IHttpContextAccessor httpContextAccessor) =>
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        public string GetSub() => _httpContextAccessor.HttpContext.User.FindFirst("sub").Value;

        public AccountId GetAccountId()
        {
            if (_httpContextAccessor.HttpContext.User.Claims.TryGetLong("sub", out var id))
                return new AccountId(id);

            throw new ArgumentException("Account id cannot be null in identity service", "Claims.sub");
        }

        public IdentityId GetIdentityId()
        {
            var identity = _httpContextAccessor.HttpContext.User.Claims.GetClaimValueOrDefault("identity")
                           ?? throw new ArgumentException("Identity claim cannot be null in identity service",
                               "Claims.identity");

            return IdentityId.Parse(identity);
        }

        public string? GetIdentityName() => _httpContextAccessor.HttpContext.User.Identity.Name;
    }
}