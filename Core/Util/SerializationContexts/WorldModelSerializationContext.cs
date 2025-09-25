using Helion.Util.Container;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helion.Util.SerializationContexts
{
    using Helion.Models;
    using Helion.Util.RandomGenerators;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = [typeof(DynamicArrayConverterFactory)]
    )]
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
    [JsonSerializable(typeof(RngMethod), TypeInfoPropertyName = "RandomMethod")]
    [JsonSerializable(typeof(IEnumerable<PlayerModel>))]
    [JsonSerializable(typeof(IEnumerable<EntityModel>))]
    [JsonSerializable(typeof(IEnumerable<SectorModel>))]
    [JsonSerializable(typeof(IEnumerable<LineModel>))]
    [JsonSerializable(typeof(IEnumerable<ISpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<SectorMoveSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<ScrollSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<LightChangeSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<LightFireFlickerDoomModel>))]
    [JsonSerializable(typeof(IEnumerable<LightFlickerDoomSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<LightPulsateSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<LightStrobeSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<PushSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<StairSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<ElevatorSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<SwitchChangeSpecialModel>))]
    [JsonSerializable(typeof(IEnumerable<SectorDamageSpecialModel>))]
    public partial class WorldModelSerializationContext : JsonSerializerContext
    {
    }
    public class DynamicArrayConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType &&
                   typeToConvert.GetGenericTypeDefinition() == typeof(DynamicArray<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var elementType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(DynamicArrayConverter<>).MakeGenericType(elementType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}

