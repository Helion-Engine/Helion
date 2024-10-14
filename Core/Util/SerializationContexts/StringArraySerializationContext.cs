namespace Helion.Util.SerializationContexts
{
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(string[]), TypeInfoPropertyName = "StringArray")]
    public partial class StringArraySerializationContext : JsonSerializerContext
    {
    }
}
