namespace Helion.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal class SpecialModelConverter : JsonConverter<ISpecialModel>
    {
        // We're keeping a dictionary of specific type names, rather than using reflection to discover all types that
        // implement the expected interface, because the reflection might complicate trimmed and/or AOT publish.
        private static readonly Dictionary<string, Type> NameToType = new()
        {
            { "Helion.Models.ElevatorSpecialModel", typeof(ElevatorSpecialModel) },
            { "Helion.Models.LightChangeSpecialModel", typeof(LightChangeSpecialModel) },
            { "Helion.Models.LightFireFlickerDoomModel", typeof(LightFireFlickerDoomModel) },
            { "Helion.Models.LightFlickerDoomSpecialModel", typeof(LightFlickerDoomSpecialModel) },
            { "Helion.Models.LightPulsateSpecialModel", typeof(LightPulsateSpecialModel) },
            { "Helion.Models.LightStrobeSpecialModel", typeof(LightStrobeSpecialModel) },
            { "Helion.Models.PushSpecialModel", typeof(PushSpecialModel) },
            { "Helion.Models.ScrollSpecialModel", typeof(ScrollSpecialModel) },
            { "Helion.Models.SectorMoveSpecialModel", typeof(SectorMoveSpecialModel) },
            { "Helion.Models.StairSpecialModel", typeof(StairSpecialModel) },
            { "Helion.Models.SwitchChangeSpecialModel", typeof(SwitchChangeSpecialModel) }
        };

        public override ISpecialModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader readerClone = reader;
            if (readerClone.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start object");
            }

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            string? propertyName = readerClone.GetString();
            if (propertyName != "$type")
            {
                throw new JsonException("Expected $type property");
            }

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string value for $type property");
            }

            string? typeName = readerClone.GetString()?.Split(",")[0];
            if (typeName == null)
            {
                throw new JsonException("Unspecified type");
            }

            if (!NameToType.TryGetValue(typeName, out Type? implementationType))
            {
                throw new JsonException($"Unknown type: {typeName}");
            }

            object? deserialized = JsonSerializer.Deserialize(ref reader, implementationType, options);
            return (ISpecialModel?)deserialized;
        }

        public override void Write(Utf8JsonWriter writer, ISpecialModel value, JsonSerializerOptions options)
        {
            // Write a "$type" property that encodes the implementation type, then serialize the data according to its
            // implementation type.

            Type type = value.GetType();

            writer.WriteStartObject();
            using (JsonDocument subDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, type, options)))
            {
                writer.WriteString("$type", $"{type.FullName}, Core");  // Imitate Newtonsoft.Json format
                foreach (var element in subDocument.RootElement.EnumerateObject())
                {
                    element.WriteTo(writer);
                }

            }
            writer.WriteEndObject();
        }
    }
}