namespace Helion.Util.SerializationContexts
{
    using Helion.Resources.Definitions.Id24;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(GameConf), TypeInfoPropertyName = "GameConf")]
    public partial class GameConfSerializationContext : JsonSerializerContext
    { 
    }
}
