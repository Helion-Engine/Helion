using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Composer;

public static class DefinitionFlagApplier
{
    public static void Apply(EntityDefinition definition, ActorFlags flags, ActorFlagProperty flagProperties)
    {
        if (flagProperties.ClearFlags ?? false)
            definition.Flags.ClearAll();

        if (flags.Monster ?? false)
        {
            definition.Flags.SetCanPass();
            definition.Flags.SetCountKill();
            definition.Flags.SetIsMonster();
            definition.Flags.SetShootable();
            definition.Flags.SetSolid();
        }

        if (flags.Projectile ?? false)
        {
            definition.Flags.SetDropoff();
            definition.Flags.SetMissile();
            definition.Flags.SetNoBlockmap();
            definition.Flags.SetNoGravity();
            definition.Flags.SetNoTeleport();
        }
               
        if (flags.ActLikeBridge != null)
            definition.Flags.SetActLikeBridge(flags.ActLikeBridge.Value);

        if (flags.Ambush != null)
            definition.Flags.SetAmbush(flags.Ambush.Value);
        
        if (flags.Boss != null)
            definition.Flags.SetBoss(flags.Boss.Value);
               
        if (flags.Corpse != null)
            definition.Flags.SetCorpse(flags.Corpse.Value);
        if (flags.CountItem != null)
            definition.Flags.SetCountItem(flags.CountItem.Value);
        if (flags.CountKill != null)
            definition.Flags.SetCountKill(flags.CountKill.Value);
        if (flags.DoHarmSpecies != null)
            definition.Flags.SetDoHarmSpecies(flags.DoHarmSpecies.Value);
        if (flags.DontFall != null)
            definition.Flags.SetDontFall(flags.DontFall.Value);
        if (flags.DontGib != null)
            definition.Flags.SetDontGib(flags.DontGib.Value);
        if (flags.Dropoff != null)
            definition.Flags.SetDropoff(flags.Dropoff.Value);
        if (flags.Dropped != null)
            definition.Flags.SetDropped(flags.Dropped.Value);
        if (flags.Float != null)
            definition.Flags.SetFloat(flags.Float.Value);
        if (flags.ForceRadiusDmg != null)
            definition.Flags.SetForceRadiusDmg(flags.ForceRadiusDmg.Value);
        if (flags.Friendly != null)
            definition.Flags.SetFriendly(flags.Friendly.Value);
        if (flags.FullVolDeath != null)
            definition.Flags.SetFullVolDeath(flags.FullVolDeath.Value);
        if (flags.Inventory.AlwaysPickup != null)
            definition.Flags.SetInventoryAlwaysPickup(flags.Inventory.AlwaysPickup.Value);       
        if (flags.Invulnerable != null)
            definition.Flags.SetInvulnerable(flags.Invulnerable.Value);
        if (flags.IsMonster != null)
            definition.Flags.SetIsMonster(flags.IsMonster.Value);
        if (flags.JustHit != null)
            definition.Flags.SetJustHit(flags.JustHit.Value);
        if (flags.MbfBouncer != null)
            definition.Flags.SetMbfBouncer(flags.MbfBouncer.Value);
        if (flags.Missile != null)
            definition.Flags.SetMissile(flags.Missile.Value);
        if (flags.MissileEvenMore != null)
            definition.Flags.SetMissileEvenMore(flags.MissileEvenMore.Value);
        if (flags.MissileMore != null)
            definition.Flags.SetMissileMore(flags.MissileMore.Value);
        if (flags.Monster != null)
        {
            definition.Flags.SetShootable();
            definition.Flags.SetCountKill();
            definition.Flags.SetSolid();
            definition.Flags.SetCanPass();
            definition.Flags.SetIsMonster();
        }
        if (flags.NoBlockmap != null)
            definition.Flags.SetNoBlockmap(flags.NoBlockmap.Value);
        if (flags.NoBlood != null)
            definition.Flags.SetNoBlood(flags.NoBlood.Value);
        if (flags.NoClip != null)
            definition.Flags.SetNoClip(flags.NoClip.Value);
        if (flags.NoFriction != null)
            definition.Flags.SetNoFriction(flags.NoFriction.Value);
        if (flags.NoGravity != null)
            definition.Flags.SetNoGravity(flags.NoGravity.Value);
        if (flags.NoRadiusDmg != null)
            definition.Flags.SetNoRadiusDmg(flags.NoRadiusDmg.Value);
        if (flags.NoSector != null)
            definition.Flags.SetNoSector(flags.NoSector.Value);
        if (flags.NoTarget != null)
            definition.Flags.SetNoTarget(flags.NoTarget.Value);
        if (flags.NotDMatch != null)
            definition.Flags.SetNotDMatch(flags.NotDMatch.Value);
        if (flags.NoTeleport != null)
            definition.Flags.SetNoTeleport(flags.NoTeleport.Value);
        if (flags.NoVerticalMeleeRange != null)
            definition.Flags.SetNoVerticalMeleeRange(flags.NoVerticalMeleeRange.Value);
        if (flags.OldRadiusDmg != null)
            definition.Flags.SetOldRadiusDmg(flags.OldRadiusDmg.Value);
        if (flags.Pickup != null)
            definition.Flags.SetPickup(flags.Pickup.Value);
        if (flags.Projectile != null)
        {
            definition.Flags.SetNoBlockmap();
            definition.Flags.SetNoGravity();
            definition.Flags.SetDropoff();
            definition.Flags.SetMissile();
            definition.Flags.SetNoTeleport();
        }
        if (flags.QuickToRetaliate != null)
            definition.Flags.SetQuickToRetaliate(flags.QuickToRetaliate.Value);
        if (flags.Randomize != null)
            definition.Flags.SetRandomizeProjectile(flags.Randomize.Value);
        if (flags.Ripper != null)
            definition.Flags.SetRipper(flags.Ripper.Value);
        if (flags.Shootable != null)
            definition.Flags.SetShootable(flags.Shootable.Value);
        if (flags.Skullfly != null)
            definition.Flags.SetSkullfly(flags.Skullfly.Value);
        if (flags.SlidesOnWalls != null)
            definition.Flags.SetSlidesOnWalls(flags.SlidesOnWalls.Value);
        if (flags.Solid != null)
            definition.Flags.SetSolid(flags.Solid.Value);
        if (flags.SpawnCeiling != null)
            definition.Flags.SetSpawnCeiling(flags.SpawnCeiling.Value);
        if (flags.Special != null)
            definition.Flags.SetSpecial(flags.Special.Value);
        if (flags.StepMissile != null)
            definition.Flags.SetStepMissile(flags.StepMissile.Value);
        if (flags.Teleport != null)
            definition.Flags.SetTeleport(flags.Teleport.Value);
        if (flags.Touchy != null)
            definition.Flags.SetTouchy(flags.Touchy.Value);
        if (flags.Weapon.MeleeWeapon != null)
            definition.Flags.SetWeaponMeleeWeapon(flags.Weapon.MeleeWeapon.Value);
        if (flags.Weapon.NoAlert != null)
            definition.Flags.SetWeaponNoAlert(flags.Weapon.NoAlert.Value);
        if (flags.Weapon.NoAutoSwitchTo != null)
            definition.Flags.SetWeaponNoAutoSwitch(flags.Weapon.NoAutoSwitchTo.Value);
        if (flags.Weapon.WimpyWeapon != null)
            definition.Flags.SetWeaponWimpyWeapon(flags.Weapon.WimpyWeapon.Value);
        if (flags.WindThrust != null)
            definition.Flags.SetWindThrust(flags.WindThrust.Value);
        if (flags.Invisible != null)
            definition.Flags.SetInvisible(flags.Invisible.Value);
        if (flags.JustAttacked != null)
            definition.Flags.SetJustAttacked(flags.JustAttacked.Value);
        if (flags.Bright != null)
            definition.Flags.SetBright(flags.Bright.Value);
        if (flags.IsTeleportSpot != null)
            definition.Flags.SetIsTeleportSpot(flags.IsTeleportSpot.Value);
        if (flags.E1M8Boss != null)
            definition.Flags.SetE1M8Boss(flags.E1M8Boss.Value);
        if (flags.E2M8Boss != null)
            definition.Flags.SetE2M8Boss(flags.E2M8Boss.Value);
        if (flags.E3M8Boss != null)
            definition.Flags.SetE3M8Boss(flags.E3M8Boss.Value);
        if (flags.E4M6Boss != null)
            definition.Flags.SetE4M6Boss(flags.E4M6Boss.Value);
        if (flags.E4M8Boss != null)
            definition.Flags.SetE4M8Boss(flags.E4M8Boss.Value);
        if (flags.FullVolSee != null)
            definition.Flags.SetFullVolSee(flags.FullVolSee.Value);
        if (flags.Map07Boss1 != null)
            definition.Flags.SetMap07Boss1(flags.Map07Boss1.Value);
        if (flags.Map07Boss2 != null)
            definition.Flags.SetMap07Boss2(flags.Map07Boss2.Value);
        if (flags.CanPass != null)
            definition.Flags.SetCanPass(flags.CanPass.Value);
        if (flags.Shadow != null)
            definition.Flags.SetShadow(flags.Shadow.Value);
        if (flags.DehExplosion != null)
            definition.Properties.RenderStyle = RenderStyle.ColorAddExplosion;
        if (flags.Stealth != null)
            definition.Flags.SetStealth(flags.Stealth.Value);
        if (flags.DontMirrorCorpse != null)
            definition.Flags.SetDontMirrorCorpse(flags.DontMirrorCorpse.Value);
    }
}
