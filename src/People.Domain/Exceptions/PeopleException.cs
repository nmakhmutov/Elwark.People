namespace People.Domain.Exceptions;

public abstract class PeopleException : Exception
{
    public string Name { get; }

    public string Code { get; }

    protected PeopleException(string name, string code, string? message = null)
        : base(message)
    {
        Name = name;
        Code = code;
    }
}
