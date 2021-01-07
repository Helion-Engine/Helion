using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionPropertyApplier
    {
        public static void Apply(EntityDefinition definition, ActorProperties properties)
        {
            if (properties.Alpha != null)
                definition.Properties.Alpha = properties.Alpha.Value;

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

            if (properties.Inventory.Icon != null)
                definition.Properties.Inventory.Icon = properties.Inventory.Icon;

            if (properties.Armor.SaveAmount.HasValue)
                definition.Properties.Armor.SaveAmount = properties.Armor.SaveAmount.Value;

            if (properties.Armor.MaxSaveAmount.HasValue)
                definition.Properties.Armor.MaxSaveAmount = properties.Armor.MaxSaveAmount.Value;

            if (properties.Armor.SavePercent.HasValue)
                definition.Properties.Armor.SavePercent = properties.Armor.SavePercent.Value;

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

            if (properties.HitObituary != null)
                definition.Properties.HitObituary = properties.HitObituary;
            if (properties.Obituary != null)
                definition.Properties.Obituary = properties.Obituary;

            if (properties.HealthProperty.LowMessage != null && properties.HealthProperty.LowMessageHealth.HasValue)
            {
                definition.Properties.HealthProperty = new Properties.Components.HealthProperty()
                {
                    LowMessage = properties.HealthProperty.LowMessage,
                    LowMessageHealth = properties.HealthProperty.LowMessageHealth.Value
                };
            }

            if (properties.DropItem.ClassName != null)
            {
                byte probability = properties.DropItem.Probability ?? Properties.Components.DropItemProperty.DefaultProbability;
                int amount = properties.DropItem.Amount ?? Properties.Components.DropItemProperty.DefaultAmount;
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

            if (properties.Weapons.SelectionOrder.HasValue)
                definition.Properties.Weapons.SelectionOrder = properties.Weapons.SelectionOrder.Value;
            if (properties.Weapons.ReadySound != null)
                definition.Properties.Weapons.ReadySound = properties.Weapons.ReadySound;
            if (properties.Weapons.UpSound != null)
                definition.Properties.Weapons.UpSound = properties.Weapons.UpSound;

            if (properties.Ammo.BackpackAmount.HasValue)
                definition.Properties.Ammo.BackpackAmount = properties.Ammo.BackpackAmount.Value;
            if (properties.Ammo.BackpackMaxAmount.HasValue)
                definition.Properties.Ammo.BackpackMaxAmount = properties.Ammo.BackpackMaxAmount.Value;
            if (properties.Ammo.DropAmount.HasValue)
                definition.Properties.Ammo.DropAmount = properties.Ammo.DropAmount.Value;

            if (properties.Powerup.Color != null)
            {
                definition.Properties.Powerup.Color = new Properties.Components.PowerupColor(properties.Powerup.Color.Color);
                if (properties.Powerup.Color.Alpha.HasValue)
                    definition.Properties.Powerup.Color.Alpha = properties.Powerup.Color.Alpha.Value;
            }
            if (properties.Powerup.Colormap != null)
            {
                definition.Properties.Powerup.Colormap = new Properties.Components.PowerupColorMap(properties.Powerup.Colormap.Destination);
                if (properties.Powerup.Colormap.Source.HasValue)
                    definition.Properties.Powerup.Colormap.Source = properties.Powerup.Colormap.Source.Value;
            }
            if (properties.Powerup.Duration.HasValue)
                definition.Properties.Powerup.Duration = properties.Powerup.Duration.Value;
            if (properties.Powerup.Mode.HasValue)
                definition.Properties.Powerup.Mode = properties.Powerup.Mode.Value;
            if (properties.Powerup.Strength.HasValue)
                definition.Properties.Powerup.Strength = properties.Powerup.Strength.Value;
            if (properties.Powerup.Type != null)
                definition.Properties.Powerup.Type = properties.Powerup.Type;
        }
    }
}