using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models
{
    public class ConfirmationModel
    {
        public ConfirmationModel(IdentityId id, Identification identification)
        {
            Id = id;
            Identification = identification;
        }

        public IdentityId Id { get; set; }
        
        public Identification Identification { get; }
    }
}