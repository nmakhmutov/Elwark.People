using System.Data.Common;
using Newtonsoft.Json;

namespace Elwark.People.Shared
{
    public static class DbDataReaderExtensions
    {
        public static T? GetNullableFieldValue<T>(this DbDataReader reader, int ordinal) where T : class =>
            reader.IsDBNull(ordinal)
                ? null
                : reader.GetFieldValue<T>(ordinal);

        public static T? GetJsonFieldValue<T>(this DbDataReader reader, int ordinal) where T : class =>
            reader.IsDBNull(ordinal)
                ? null
                : JsonConvert.DeserializeObject<T>(reader.GetFieldValue<string>(ordinal));
    }
}