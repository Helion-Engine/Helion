using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionPropertyApplier
    {
        public static void Apply(EntityDefinition definition, ActorProperties properties)
        {
            // TODO
            if (properties.Health != null)
                definition.Properties.Health = properties.Health.Value;
            // TODO
            if (properties.Height != null)
                definition.Properties.Height = properties.Height.Value;
            // TODO
            if (properties.MaxStepHeight != null)
                definition.Properties.MaxStepHeight = properties.MaxStepHeight.Value;
            // TODO
            if (properties.Radius != null)
                definition.Properties.Radius = properties.Radius.Value;

            if (properties.Speed != null)
                definition.Properties.Speed = properties.Speed.Value;

            if (properties.Damage.Low != null)
                definition.Properties.Damage = properties.Damage.Low.Value;

            if (properties.PainChance != null)
                definition.Properties.PainChance = (int)properties.PainChance.Value.Value;

            if (properties.ProjectileKickBack != null)
                definition.Properties.ProjectileKickBack = properties.ProjectileKickBack.Value;
        }
    }
}