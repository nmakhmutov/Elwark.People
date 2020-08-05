using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Elwark.People.Shared
{
    public static class ElwarkJsonSettings
    {
        static ElwarkJsonSettings() =>
            Value = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Converters =
                {
                    new IsoDateTimeConverter(),
                    new StringEnumConverter()
                }
            };

        public static JsonSerializerSettings Value { get; }
    }
}