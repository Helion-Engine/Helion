namespace Helion.Util.SerializationContexts
{
    using Helion.Models;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true)]
    [JsonSerializable(typeof(WorldModel), TypeInfoPropertyName = "WorldModel")]
    [JsonSerializable(typeof(ISpecialModel), TypeInfoPropertyName = "ISpecialModel")]
    [JsonSerializable(typeof(ElevatorSpecialModel), TypeInfoPropertyName = "ElevatorSpecialModel")]
    [JsonSerializable(typeof(LightChangeSpecialModel), TypeInfoPropertyName = "LightChangeSpecialModel")]
    [JsonSerializable(typeof(LightFireFlickerDoomModel), TypeInfoPropertyName = "LightFireFlickerDoomModel")]
    [JsonSerializable(typeof(LightFlickerDoomSpecialModel), TypeInfoPropertyName = "LightFlickerDoomSpecialModel")]
    [JsonSerializable(typeof(LightPulsateSpecialModel), TypeInfoPropertyName = "LightPulsateSpecialModel")]
    [JsonSerializable(typeof(LightStrobeSpecialModel), TypeInfoPropertyName = "LightStrobeSpecialModel")]
    [JsonSerializable(typeof(PushSpecialModel), TypeInfoPropertyName = "PushSpecialModel")]
    [JsonSerializable(typeof(ScrollSpecialModel), TypeInfoPropertyName = "ScrollSpecialModel")]
    [JsonSerializable(typeof(SectorMoveSpecialModel), TypeInfoPropertyName = "SectorMoveSpecialModel")]
    [JsonSerializable(typeof(StairSpecialModel), TypeInfoPropertyName = "StairSpecialModel")]
    [JsonSerializable(typeof(SwitchChangeSpecialModel), TypeInfoPropertyName = "SwitchChangeSpecialModel")]
    public partial class WorldModelSerializationContext : JsonSerializerContext
    {
    }
}
