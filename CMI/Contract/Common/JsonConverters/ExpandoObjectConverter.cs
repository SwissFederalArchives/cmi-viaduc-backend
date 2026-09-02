using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMI.Contract.Common.JsonConverters;

public class ExpandoObjectConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ReadAsExpando(ref reader, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            JsonTokenType.String => ReadString(ref reader),
            JsonTokenType.Number => reader.TryGetInt64(out long l) ? l : reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            _ => throw new JsonException()
        };
    }

    private ExpandoObject ReadAsExpando(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var expando = (IDictionary<string, object>) new ExpandoObject();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return (ExpandoObject) expando;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();
            var key = reader.GetString();
            reader.Read();
            expando[key] = Read(ref reader, typeof(object), options);
        }
        throw new JsonException();
    }

    private List<object> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var list = new List<object>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            list.Add(Read(ref reader, typeof(object), options));
        return list;
    }

    private object ReadString(ref Utf8JsonReader reader)
    {
        // Analog zu Newtonsoft reader.Value: DateTime/DateTimeOffset erkennen
        if (reader.TryGetDateTime(out DateTime dt))
            return dt;
        if (reader.TryGetDateTimeOffset(out DateTimeOffset dto))
            return dto;
        return reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is ExpandoObject expando)
        {
            WriteDictionary(writer, (IDictionary<string, object>) expando, options);
            return;
        }

        // Primitive Typen direkt schreiben – NICHT via JsonSerializer.Serialize
        // sonst greift wieder dieser Converter (Endlosrekursion / falscher Pfad)
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case int i:
                writer.WriteNumberValue(i);
                break;

            case long l:
                writer.WriteNumberValue(l);
                break;

            case double d:
                writer.WriteNumberValue(d);
                break;

            case float f:
                writer.WriteNumberValue(f);
                break;
            case ElasticFloat ef:
                writer.WriteNumberValue(ef.Value);
                break;

            case decimal dec:
                writer.WriteNumberValue(dec);
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case DateTime dt:
                writer.WriteStringValue(dt);
                break;

            case Guid g:
                writer.WriteStringValue(g);
                break;

            case IEnumerable<object> list:
                writer.WriteStartArray();
                foreach (var item in list)
                {
                    Write(writer, item, options);
                }

                writer.WriteEndArray();
                break;

            case IDictionary<string, object> dict:
                WriteDictionary(writer, dict, options);
                break;

            default:
                // Für alle anderen bekannten Typen: konkreten Typ verwenden
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }

    private void WriteDictionary(Utf8JsonWriter writer, IDictionary<string, object> dict, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in dict)
        {
            writer.WritePropertyName(kvp.Key);
            if (kvp.Value is null)
                writer.WriteNullValue();
            else
                Write(writer, kvp.Value, options); // rekursiv
        }
        writer.WriteEndObject();
    }
}