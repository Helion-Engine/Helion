namespace Helion.Util.SerializationContexts
{
    using Helion.Models;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(DemoModel), TypeInfoPropertyName = "DemoModel")]
    public partial class DemoModelSerializationContext : JsonSerializerContext
    {
    }
}
