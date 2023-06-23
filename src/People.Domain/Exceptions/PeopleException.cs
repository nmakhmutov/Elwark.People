namespace People.Domain.Exceptions;

public abstract class PeopleException : Exception
{
    protected PeopleException(string name, string code, string? message = null)
        : base(message)
    {
        Name = name;
        Code = code;
    }

    public string Name { get; }

    public string Code { get; }
}
