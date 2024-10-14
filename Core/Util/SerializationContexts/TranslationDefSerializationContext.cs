namespace Helion.Util.SerializationContexts
{
    using Helion.Resources.Definitions.Id24;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(TranslationDef), TypeInfoPropertyName = "TranslationDef")]
    public partial class TranslationDefSerializationContext : JsonSerializerContext
    {
    }
}
