using People.Domain.SeedWork;

// ReSharper disable NotAccessedField.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Entities;

public sealed class ExternalConnection : Entity<Ulid>
{
    private DateTime _createdAt;

    private ExternalConnection(
        ExternalService type,
        string identity,
        string? firstName,
        string? lastName,
        DateTime createdAt
    )
    {
        Id = Ulid.NewUlid();
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
