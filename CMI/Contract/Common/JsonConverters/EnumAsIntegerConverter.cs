using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMI.Contract.Common.JsonConverters
{

    // Eigener Converter der Enum als Integer serialisiert
    public class EnumAsIntegerConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return (T) Enum.ToObject(typeToConvert, reader.GetInt32());

            // Fallback für bereits als String gespeicherte Werte im Index
            if (reader.TokenType == JsonTokenType.String)
                if (Enum.TryParse<T>(reader.GetString(), out var result))
                    return result;

            throw new JsonException($"Kann Wert nicht zu {typeToConvert.Name} konvertieren");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }
}
