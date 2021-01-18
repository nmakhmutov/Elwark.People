using System;
using Microsoft.AspNetCore.Http;

namespace People.Gateway.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityService(IHttpContextAccessor httpContextAccessor) =>
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        public long GetAccountId()
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirst(x => x.Type == "sub");
            if (sub is not null)
                return long.Parse(sub.Value);

            throw new ArgumentException("Account id cannot be null in identity service");
        }
    }
}