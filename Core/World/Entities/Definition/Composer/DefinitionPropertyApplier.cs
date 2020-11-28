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

            if (properties.Inventory.MaxAmount != null)
                definition.Properties.Inventory.MaxAmount = properties.Inventory.MaxAmount.Value;

            if (properties.Inventory.PickupSound != null)
                definition.Properties.Inventory.PickupSound = properties.Inventory.PickupSound;

            if (properties.Inventory.PickupMessage != null)
                definition.Properties.Inventory.PickupMessage = properties.Inventory.PickupMessage;

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

            if (properties.MeleeRange != null)
                definition.Properties.MeleeRange = properties.MeleeRange.Value;

            if (properties.MeleeDamage != null)
                definition.Properties.MeleeDamage = properties.MeleeDamage.Value;

            if (properties.MeleeThreshold.HasValue)
                definition.Properties.MeleeThreshold = properties.MeleeThreshold.Value;

            if (properties.MaxTargetRange.HasValue)
                definition.Properties.MaxTargetRange = properties.MaxTargetRange.Value;

            if (properties.MinMissileChance.HasValue)
                definition.Properties.MinMissileChance = properties.MinMissileChance.Value;

            if (properties.ReactionTime.HasValue)
                definition.Properties.ReactionTime = properties.ReactionTime.Value;

            if (properties.Threshold.HasValue)
                definition.Properties.Threshold = properties.Threshold.Value;

            if (properties.DefThreshold.HasValue)
                definition.Properties.DefThreshold = properties.DefThreshold.Value;

            if (properties.DeathSound != null)
                definition.Properties.DeathSound = properties.DeathSound;

            if (properties.PainSound != null)
                definition.Properties.PainSound = properties.PainSound;

            if (properties.SeeSound != null)
                definition.Properties.SeeSound = properties.SeeSound;

            if (properties.AttackSound != null)
                definition.Properties.AttackSound = properties.AttackSound;

            if (properties.MeleeSound != null)
                definition.Properties.MeleeSound = properties.MeleeSound;

            if (properties.ActiveSound != null)
                definition.Properties.ActiveSound = properties.ActiveSound;

            if (properties.DropItem.ClassName != null)
            {
                byte probability = properties.DropItem.Probability.HasValue ? properties.DropItem.Probability.Value : Properties.Components.DropItemProperty.DefaultProbability;
                int amount = properties.DropItem.Amount.HasValue ? properties.DropItem.Amount.Value : Properties.Components.DropItemProperty.DefaultAmount;
                definition.Properties.DropItem = new Properties.Components.DropItemProperty(properties.DropItem.ClassName,
                    probability, amount);
            }

            if (properties.Weapons.AmmoType != null)
                definition.Properties.Weapons.AmmoType = properties.Weapons.AmmoType;
            if (properties.Weapons.AmmoType1 != null)
                definition.Properties.Weapons.AmmoType1 = properties.Weapons.AmmoType1;
            if (properties.Weapons.AmmoType2 != null)
                definition.Properties.Weapons.AmmoType2 = properties.Weapons.AmmoType2;

            if (properties.Weapons.AmmoUse.HasValue)
                definition.Properties.Weapons.AmmoUse = properties.Weapons.AmmoUse.Value;
            if (properties.Weapons.AmmoUse1.HasValue)
                definition.Properties.Weapons.AmmoUse1 = properties.Weapons.AmmoUse1.Value;
            if (properties.Weapons.AmmoUse2.HasValue)
                definition.Properties.Weapons.AmmoUse2 = properties.Weapons.AmmoUse2.Value;

            if (properties.Weapons.AmmoGive.HasValue)
                definition.Properties.Weapons.AmmoGive = properties.Weapons.AmmoGive.Value;
            if (properties.Weapons.AmmoGive1.HasValue)
                definition.Properties.Weapons.AmmoGive1 = properties.Weapons.AmmoGive1.Value;
            if (properties.Weapons.AmmoGive2.HasValue)
                definition.Properties.Weapons.AmmoGive2 = properties.Weapons.AmmoGive2.Value;
        }
    }
}