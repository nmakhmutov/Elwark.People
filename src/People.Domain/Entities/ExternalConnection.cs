using People.Domain.SeedWork;

// ReSharper disable NotAccessedField.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Entities;

public enum ExternalService : byte
{
    Unknown = 0,
    Google = 1,
    Microsoft = 2
}

public sealed class ExternalConnection : Entity<Guid>
{
    private DateTime _createdAt;

    private ExternalConnection(ExternalService type, string identity, string? firstName, string? lastName,
        DateTime createdAt)
    {
        Type = type;
        Identity = identity;
        FirstName = firstName;
        LastName = lastName;
        _createdAt = createdAt;
    }

    public ExternalService Type { get; private set; }

    public string Identity { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public static ExternalConnection Google(string identity, string? firstName, string? lastName, DateTime now) =>
        new(ExternalService.Google, identity, firstName, lastName, now);

    public static ExternalConnection Microsoft(string identity, string? firstName, string? lastName, DateTime now) =>
        new(ExternalService.Microsoft, identity, firstName, lastName, now);
}
