using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ingestion.JsonConverters
{
    public class TimeSpanMinutesConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeSpan.FromMinutes(reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
