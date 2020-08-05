namespace Elwark.People.Domain.ErrorCodes
{
    public enum IdentificationError
    {
        AlreadyRegistered,
        AlreadyAdded,
        AlreadyConfirmed,
        NotConfirmed,
        NotFound,
        Forbidden,
        LastIdentity,
        PrimaryEmail
    }
}