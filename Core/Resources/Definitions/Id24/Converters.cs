namespace Helion.Resources.Definitions.Id24;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Helion.Resources.Definitions.Compatibility;

public class ByteArrayConverter : JsonConverter<byte[]>
{
    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new Exception($"Expected StartArray, got {reader.TokenType}");

        List<byte> bytes = new List<byte>();

        while (reader.Read() && reader.TokenType == JsonTokenType.Number)
        {
            bytes.Add(reader.GetByte());
        }

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new Exception($"Expected EndArray, got {reader.TokenType}");

        return bytes.ToArray();
    }
}

public class OptionsConverter : JsonConverter<Options>
{
    public override void Write(Utf8JsonWriter writer, Options value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override Options? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = (reader.TokenType == JsonTokenType.String)
            ? reader.GetString()
            : null;
        return new Options(token);
    }
}

