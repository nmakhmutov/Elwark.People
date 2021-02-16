using System;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace People.Infrastructure.Kafka
{
    internal sealed class KafkaDataConverter<T> : ISerializer<T>, IDeserializer<T>
    {
        public static KafkaDataConverter<T> Instance => new();
        
        private readonly Type _null = typeof(Null);
        private readonly Type _ignore = typeof(Ignore);

        private KafkaDataConverter(){}
        
        public byte[] Serialize(T data, SerializationContext context)
        {
            var type = typeof(T);
            
            if (type == _null)
                return Array.Empty<byte>();
            
            if(type == _ignore)
                throw new NotSupportedException("Ignore type not supported");

            var json = JsonConvert.SerializeObject(data);

            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            var type = typeof(T);
            
            if (type == _null || type == _ignore)
                return default!;

            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}