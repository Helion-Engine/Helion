using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionPropertyApplier
    {
        public static void Apply(EntityDefinition definition, ActorProperties properties)
        {
            // TODO
            if (properties.Health != null)
                definition.Properties.Height = properties.Health.Value;
            // TODO
            if (properties.Height != null)
                definition.Properties.Height = properties.Height.Value;
            // TODO
            if (properties.MaxStepHeight != null)
                definition.Properties.MaxStepHeight = properties.MaxStepHeight.Value;
            // TODO
            if (properties.Radius != null)
                definition.Properties.Radius = properties.Radius.Value;
            // TODO
        }
    }
}