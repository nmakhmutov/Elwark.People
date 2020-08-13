using System.Diagnostics;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.Models
{
    public class IdentityActiveResponse
    {
        [JsonConstructor, DebuggerStepThrough]
        private IdentityActiveResponse(bool isActive) =>
            IsActive = isActive;

        public static IdentityActiveResponse Activated => new IdentityActiveResponse(true);

        public static IdentityActiveResponse Deactivated => new IdentityActiveResponse(false);

        public bool IsActive { get; }
    }
}