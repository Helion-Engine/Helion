using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helion.Util.Container;

public class DynamicArrayConverter<T> : JsonConverter<DynamicArray<T>>
{
    public override DynamicArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = JsonSerializer.Deserialize<T[]>(ref reader, options);
        return new DynamicArray<T>(items);
    }

    public override void Write(Utf8JsonWriter writer, DynamicArray<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Data.Take(value.Length), options);
    }
}
