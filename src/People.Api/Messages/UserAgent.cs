// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class UserAgent
{
    public string? GetValue() =>
        string.IsNullOrWhiteSpace(Value) ? null : Value;
}
