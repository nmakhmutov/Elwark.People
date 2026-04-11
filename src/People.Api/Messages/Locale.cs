// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class Locale
{
    public Domain.ValueObjects.Locale ToLocale() =>
        Domain.ValueObjects.Locale.Parse(Value);

    public static Locale Create(Domain.ValueObjects.Locale locale) =>
        new() { Value = locale.ToString() };
}
