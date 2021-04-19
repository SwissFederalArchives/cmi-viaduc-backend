using System;
using Newtonsoft.Json;

namespace CMI.Contract.Common.JsonConverters
{
    public class LongToTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override void WriteJson(JsonWriter writer, TimeSpan? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.Ticks ?? 0);
        }

        public override TimeSpan? ReadJson(JsonReader reader, Type objectType, TimeSpan? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var ticks = reader.Value != null ? (long?) Convert.ToInt64(reader.Value) : null;
            if (ticks.HasValue && ticks.Value > 0)
            {
                return TimeSpan.FromTicks(ticks.Value);
            }

            return null;
        }
    }
}