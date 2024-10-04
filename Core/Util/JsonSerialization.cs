using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helion.Util;

public static class JsonSerialization
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public static T? Deserialize<T>(string input)
    {
        return JsonSerializer.Deserialize<T>(input, DefaultOptions);
    }

    public static string Serialize<T>(T input)
    {
        return JsonSerializer.Serialize<T>(input, DefaultOptions);
    }
}
