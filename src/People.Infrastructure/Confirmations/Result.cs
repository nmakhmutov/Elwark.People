namespace People.Infrastructure.Confirmations;

public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly Exception? _exception;
    private readonly T? _value;

    public static implicit operator Result<T>(T value) =>
        new(value);

    public Result(T value)
    {
        HasException = false;
        _value = value;
        _exception = null;
    }

    public Result(Exception exception)
    {
        HasException = true;
        _exception = exception;
        _value = default;
    }

    public bool HasException { get; }

    public T Value =>
        _value ?? throw new NullReferenceException("Result value is null");

    public Exception Exception =>
        _exception ?? throw new NullReferenceException("Result exception is null");

    public T GetOrThrow()
    {
        if (_value is not null)
            return _value;

        throw Exception;
    }

    public override string ToString() =>
        (HasException ? _exception?.GetType().Name : _value?.ToString()) ?? "(null)";

    public bool Equals(Result<T> other) =>
        Equals(_value, other._value) && Equals(_exception, other._exception);

    public override bool Equals(object? obj) =>
        obj is Result<T> result && Equals(result);

    public override int GetHashCode() =>
        HashCode.Combine(HasException, _exception, _value);

    public static bool operator ==(Result<T> left, Result<T> right) =>
        left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) =>
        !left.Equals(right);
}
