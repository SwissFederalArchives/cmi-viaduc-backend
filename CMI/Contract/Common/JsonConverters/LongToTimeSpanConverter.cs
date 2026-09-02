using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMI.Contract.Common.JsonConverters
{
    public class LongToTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value?.Ticks ?? 0);
        }

        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetInt64(out var ticks))
            {
                return TimeSpan.FromTicks(ticks);
            }

            return null;
        }
    }
}