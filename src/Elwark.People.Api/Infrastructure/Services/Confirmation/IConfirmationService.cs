using System;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Infrastructure.Services.Confirmation
{
    public interface IConfirmationService
    {
        string WriteToken(Guid confirmationId, IdentityId identityId, ConfirmationType type, long code);

        ConfirmationData ReadToken(string token);
    }
}