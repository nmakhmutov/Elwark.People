// ReSharper disable CheckNamespace

namespace People.Grpc.People;

public partial class Language
{
    public Domain.ValueObjects.Language ToLanguage() =>
        Domain.ValueObjects.Language.Parse(Value);

    public static Language Create(Domain.ValueObjects.Language language) =>
        new()
        {
            Value = language.ToString()
        };
}
