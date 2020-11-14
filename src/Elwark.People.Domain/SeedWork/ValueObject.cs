using System.Collections.Generic;
using System.Linq;

namespace Elwark.People.Domain.SeedWork
{
    public abstract record ValueObject
    {
        protected abstract IEnumerable<object?> GetAtomicValues();

        public override int GetHashCode() =>
            GetAtomicValues()
                .Select(x => x is null ? 0 : x.GetHashCode())
                .Aggregate((x, y) => x ^ y);
    }
}