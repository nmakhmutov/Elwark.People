using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class ConfirmationResponse
    {
        public ConfirmationResponse(Identification identification) =>
            Identification = identification;

        public Identification Identification { get; }
    }
}