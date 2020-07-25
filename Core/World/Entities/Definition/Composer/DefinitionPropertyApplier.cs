using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionPropertyApplier
    {
        public static void Apply(EntityDefinition definition, ActorProperties properties)
        {
            if (properties.Damage.Value != null)
            {
                definition.Properties.Damage.Value = properties.Damage.Value.Value;
                definition.Properties.Damage.Exact = properties.Damage.Exact ?? false;
            }

            if (properties.Health != null)
                definition.Properties.Health = properties.Health.Value;

            if (properties.Height != null)
                definition.Properties.Height = properties.Height.Value;

            if (properties.Inventory.Amount != null)
                definition.Properties.Inventory.Amount = properties.Inventory.Amount.Value;

            if (properties.MaxStepHeight != null)
                definition.Properties.MaxStepHeight = properties.MaxStepHeight.Value;

            if (properties.PainChance != null)
                definition.Properties.PainChance = (int)properties.PainChance.Value.Value;
            
            if (properties.ProjectileKickBack != null)
                definition.Properties.ProjectileKickBack = properties.ProjectileKickBack.Value;

            if (properties.Radius != null)
                definition.Properties.Radius = properties.Radius.Value;

            if (properties.Speed != null)
                definition.Properties.Speed = properties.Speed.Value;

            if (properties.Mass != null)
                definition.Properties.Mass = properties.Mass.Value;

            if (properties.DeathSound != null)
                definition.Properties.DeathSound = properties.DeathSound;

            if (properties.PainSound != null)
                definition.Properties.PainSound = properties.PainSound;
        }
    }
}