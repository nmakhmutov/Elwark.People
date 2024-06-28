using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace People.Infrastructure.ValueConverters;

internal sealed class UlidConverter : ValueConverter<Ulid, Guid>
{
    public UlidConverter()
        : base(x => x.ToGuid(), x => new Ulid(x))
    {
    }
}
