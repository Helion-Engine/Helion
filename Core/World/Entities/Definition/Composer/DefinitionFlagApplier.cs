using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer;

public static class DefinitionFlagApplier
{
    public static void Apply(EntityDefinition definition, ActorFlags flags, ActorFlagProperty flagProperties)
    {
        if (flagProperties.ClearFlags ?? false)
            definition.Flags.ClearAll();

        if (flags.Monster ?? false)
        {
            definition.Flags.CanPass = true;
            definition.Flags.CountKill = true;
            definition.Flags.IsMonster = true;
            definition.Flags.Shootable = true;
            definition.Flags.Solid = true;
        }

        if (flags.Projectile ?? false)
        {
            definition.Flags.Dropoff = true;
            definition.Flags.Missile = true;
            definition.Flags.NoBlockmap = true;
            definition.Flags.NoGravity = true;
            definition.Flags.NoTeleport = true;
        }

       
        if (flags.ActLikeBridge != null)
            definition.Flags.ActLikeBridge = flags.ActLikeBridge.Value;


        if (flags.Ambush != null)
            definition.Flags.Ambush = flags.Ambush.Value;
        
        if (flags.Boss != null)
            definition.Flags.Boss = flags.Boss.Value;
       
        
        if (flags.Corpse != null)
            definition.Flags.Corpse = flags.Corpse.Value;
        if (flags.CountItem != null)
            definition.Flags.CountItem = flags.CountItem.Value;
        if (flags.CountKill != null)
            definition.Flags.CountKill = flags.CountKill.Value;
        if (flags.DoHarmSpecies != null)
            definition.Flags.DoHarmSpecies = flags.DoHarmSpecies.Value;
        if (flags.DontFall != null)
            definition.Flags.DontFall = flags.DontFall.Value;
        if (flags.DontGib != null)
            definition.Flags.DontGib = flags.DontGib.Value;
        if (flags.Dropoff != null)
            definition.Flags.Dropoff = flags.Dropoff.Value;
        if (flags.Dropped != null)
            definition.Flags.Dropped = flags.Dropped.Value;
        if (flags.Float != null)
            definition.Flags.Float = flags.Float.Value;
        if (flags.ForceRadiusDmg != null)
            definition.Flags.ForceRadiusDmg = flags.ForceRadiusDmg.Value;
        if (flags.Friendly != null)
            definition.Flags.Friendly = flags.Friendly.Value;
        if (flags.FullVolDeath != null)
            definition.Flags.FullVolDeath = flags.FullVolDeath.Value;
        if (flags.Inventory.AlwaysPickup != null)
            definition.Flags.InventoryAlwaysPickup = flags.Inventory.AlwaysPickup.Value;       
        if (flags.Invulnerable != null)
            definition.Flags.Invulnerable = flags.Invulnerable.Value;
        if (flags.IsMonster != null)
            definition.Flags.IsMonster = flags.IsMonster.Value;
        if (flags.JustHit != null)
            definition.Flags.JustHit = flags.JustHit.Value;
        if (flags.MbfBouncer != null)
            definition.Flags.MbfBouncer = flags.MbfBouncer.Value;
        if (flags.Missile != null)
            definition.Flags.Missile = flags.Missile.Value;
        if (flags.MissileEvenMore != null)
            definition.Flags.MissileEvenMore = flags.MissileEvenMore.Value;
        if (flags.MissileMore != null)
            definition.Flags.MissileMore = flags.MissileMore.Value;
        if (flags.Monster != null)
        {
            definition.Flags.Shootable = true;
            definition.Flags.CountKill = true;
            definition.Flags.Solid = true;
            definition.Flags.CanPass = true;
            definition.Flags.IsMonster = true;
        }
        if (flags.NoBlockmap != null)
            definition.Flags.NoBlockmap = flags.NoBlockmap.Value;
        if (flags.NoBlood != null)
            definition.Flags.NoBlood = flags.NoBlood.Value;
        if (flags.NoClip != null)
            definition.Flags.NoClip = flags.NoClip.Value;
        if (flags.NoFriction != null)
            definition.Flags.NoFriction = flags.NoFriction.Value;
        if (flags.NoGravity != null)
            definition.Flags.NoGravity = flags.NoGravity.Value;
        if (flags.NoRadiusDmg != null)
            definition.Flags.NoRadiusDmg = flags.NoRadiusDmg.Value;
        if (flags.NoSector != null)
            definition.Flags.NoSector = flags.NoSector.Value;
        if (flags.NoTarget != null)
            definition.Flags.NoTarget = flags.NoTarget.Value;
        if (flags.NotDMatch != null)
            definition.Flags.NotDMatch = flags.NotDMatch.Value;
        if (flags.NoTeleport != null)
            definition.Flags.NoTeleport = flags.NoTeleport.Value;
        if (flags.NoVerticalMeleeRange != null)
            definition.Flags.NoVerticalMeleeRange = flags.NoVerticalMeleeRange.Value;
        if (flags.OldRadiusDmg != null)
            definition.Flags.OldRadiusDmg = flags.OldRadiusDmg.Value;
        if (flags.Pickup != null)
            definition.Flags.Pickup = flags.Pickup.Value;
        if (flags.Projectile != null)
        {
            definition.Flags.NoBlockmap = true;
            definition.Flags.NoGravity = true;
            definition.Flags.Dropoff = true;
            definition.Flags.Missile = true;
            definition.Flags.NoTeleport = true;
        }
        if (flags.QuickToRetaliate != null)
            definition.Flags.QuickToRetaliate = flags.QuickToRetaliate.Value;
        if (flags.Randomize != null)
            definition.Flags.Randomize = flags.Randomize.Value;
        if (flags.Ripper != null)
            definition.Flags.Ripper = flags.Ripper.Value;
        if (flags.Shootable != null)
            definition.Flags.Shootable = flags.Shootable.Value;
        if (flags.Skullfly != null)
            definition.Flags.Skullfly = flags.Skullfly.Value;
        if (flags.SlidesOnWalls != null)
            definition.Flags.SlidesOnWalls = flags.SlidesOnWalls.Value;
        if (flags.Solid != null)
            definition.Flags.Solid = flags.Solid.Value;
        if (flags.SpawnCeiling != null)
            definition.Flags.SpawnCeiling = flags.SpawnCeiling.Value;
        if (flags.Special != null)
            definition.Flags.Special = flags.Special.Value;
        if (flags.StepMissile != null)
            definition.Flags.StepMissile = flags.StepMissile.Value;
        if (flags.Teleport != null)
            definition.Flags.Teleport = flags.Teleport.Value;
        if (flags.Touchy != null)
            definition.Flags.Touchy = flags.Touchy.Value;
        if (flags.Weapon.MeleeWeapon != null)
            definition.Flags.WeaponMeleeWeapon = flags.Weapon.MeleeWeapon.Value;
        if (flags.Weapon.NoAlert != null)
            definition.Flags.WeaponNoAlert = flags.Weapon.NoAlert.Value;
        if (flags.Weapon.NoAutoSwitchTo != null)
            definition.Flags.WeaponNoAutoSwitch = flags.Weapon.NoAutoSwitchTo.Value;
        if (flags.Weapon.WimpyWeapon != null)
            definition.Flags.WeaponWimpyWeapon = flags.Weapon.WimpyWeapon.Value;
        if (flags.WindThrust != null)
            definition.Flags.WindThrust = flags.WindThrust.Value;
        if (flags.Invisible != null)
            definition.Flags.Invisible = flags.Invisible.Value;
        if (flags.JustAttacked != null)
            definition.Flags.JustAttacked = flags.JustAttacked.Value;
        if (flags.Bright != null)
            definition.Flags.Bright = flags.Bright.Value;
        if (flags.IsTeleportSpot != null)
            definition.Flags.IsTeleportSpot = flags.IsTeleportSpot.Value;
        if (flags.E1M8Boss != null)
            definition.Flags.E1M8Boss = flags.E1M8Boss.Value;
        if (flags.E2M8Boss != null)
            definition.Flags.E2M8Boss = flags.E2M8Boss.Value;
        if (flags.E3M8Boss != null)
            definition.Flags.E3M8Boss = flags.E3M8Boss.Value;
        if (flags.E4M6Boss != null)
            definition.Flags.E3M8Boss = flags.E4M6Boss.Value;
        if (flags.E4M8Boss != null)
            definition.Flags.E4M8Boss = flags.E4M8Boss.Value;
        if (flags.FullVolSee != null)
            definition.Flags.FullVolSee = flags.FullVolSee.Value;
        if (flags.Map07Boss1 != null)
            definition.Flags.Map07Boss1 = flags.Map07Boss1.Value;
        if (flags.Map07Boss2 != null)
            definition.Flags.Map07Boss2 = flags.Map07Boss2.Value;
        if (flags.CanPass != null)
            definition.Flags.CanPass = flags.CanPass.Value;
        if (flags.Shadow != null)
            definition.Flags.Shadow = flags.Shadow.Value;
    }
}
