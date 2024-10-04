namespace Helion.Util.SerializationContexts
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(List<string>), TypeInfoPropertyName = "StringList")]
    public partial class StringListSerializationContext : JsonSerializerContext
    {
    }
}
