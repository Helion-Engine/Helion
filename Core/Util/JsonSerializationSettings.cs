using Newtonsoft.Json;

namespace Helion.Util;

public class JsonSerializationSettings
{
    public static readonly JsonSerializerSettings IgnoreNull = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };
}
