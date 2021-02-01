using System;
using MongoDB.Bson;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Infrastructure.Timezones
{
    public sealed record Timezone(ObjectId Id, string Name, TimeSpan Offset);
}