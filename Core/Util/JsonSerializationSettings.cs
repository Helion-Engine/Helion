using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helion.Util;

public static  class JsonSerializationSettings
{
    public static readonly JsonSerializerOptions IgnoreNull = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true
    };
}
