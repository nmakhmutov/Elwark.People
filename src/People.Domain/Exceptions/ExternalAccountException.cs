using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Domain.Exceptions;

public sealed class ExternalAccountException : PeopleException
{
    public ExternalAccountException(ExternalService service, string identity, string code, string? message = null)
        : base(nameof(ExternalAccountException), code, message)
    {
        Service = service;
        Identity = identity;
    }

    public ExternalService Service { get; }

    public string Identity { get; }

    public static ExternalAccountException NotFound(ExternalService type, string identity) =>
        new(type, identity, nameof(NotFound), $"External account '{type} ({identity})' not found");

    public static ExternalAccountException AlreadyCreated(ExternalService type, string identity) =>
        new(type, identity, nameof(AlreadyCreated), $"External account '{type} ({identity})' already created");
}
