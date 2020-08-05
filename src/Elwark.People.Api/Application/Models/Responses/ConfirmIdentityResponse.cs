using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class ConfirmIdentityResponse
    {
        public ConfirmIdentityResponse(IdentityId identityId) =>
            IdentityId = identityId;

        public IdentityId IdentityId { get; }
    }
}