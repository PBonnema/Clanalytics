using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrateDatabase.JsonConverters
{
    public class TimeSpanParseConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeSpan.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
