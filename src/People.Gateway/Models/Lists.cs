using System.Collections.Generic;

namespace People.Gateway.Models
{
    internal sealed record Lists(IEnumerable<Country> Countries, IEnumerable<Timezone> Timezones);
}