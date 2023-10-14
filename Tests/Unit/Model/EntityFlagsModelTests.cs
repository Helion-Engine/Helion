using FluentAssertions;
using Helion.Models;
using Helion.World.Entities.Definition.Flags;
using Xunit;

namespace Helion.Tests.Unit.Model;

public class EntityFlagsModelTests
{
    [Fact(DisplayName = "EntityFlagsModel conversion (all false)")]
    public void TestAllFalseFlags()
    {
        EntityFlags entityFlags = new EntityFlags();
        EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
        EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

        backToEntityFlags.Equals(entityFlags).Should().BeTrue();
    }

    [Fact(DisplayName = "EntityFlagsModel conversion (all true)")]
    public void TestAllTrueFlags()
    {
        EntityFlags entityFlags = new EntityFlags();
        entityFlags.ActLikeBridge = true;
        entityFlags.Ambush = true;
        entityFlags.Boss = true;
        entityFlags.Bright = true;
        entityFlags.Corpse = true;
        entityFlags.CountItem = true;
        entityFlags.CountKill = true;
        entityFlags.DoHarmSpecies = true;
        entityFlags.DontFall = true;
        entityFlags.DontGib = true;
        entityFlags.Dropoff = true;
        entityFlags.Dropped = true;
        entityFlags.Float = true;
        entityFlags.ForceRadiusDmg = true;
        entityFlags.Friendly = true;
        entityFlags.FullVolDeath = true;       
        entityFlags.Invisible = true;
        entityFlags.Invulnerable = true;
        entityFlags.IsMonster = true;
        entityFlags.IsTeleportSpot = true;
        entityFlags.JustAttacked = true;
        entityFlags.JustHit = true;
        entityFlags.MbfBouncer = true;
        entityFlags.Missile = true;
        entityFlags.MissileEvenMore = true;
        entityFlags.MissileMore = true;        
        entityFlags.NoBlockmap = true;
        entityFlags.NoBlood = true;
        entityFlags.NoClip = true;
        entityFlags.NoFriction = true;
        entityFlags.NoGravity = true;
        entityFlags.NoRadiusDmg = true;
        entityFlags.NoSector = true;
        entityFlags.NoTarget = true;
        entityFlags.NotDMatch = true;
        entityFlags.NoTeleport = true;
        entityFlags.NoVerticalMeleeRange = true;
        entityFlags.OldRadiusDmg = true;
        entityFlags.Pickup = true;
        entityFlags.QuickToRetaliate = true;
        entityFlags.Randomize = true;
        entityFlags.Ripper = true;
        entityFlags.Shadow = true;
        entityFlags.Shootable = true;
        entityFlags.Skullfly = true;
        entityFlags.SlidesOnWalls = true;
        entityFlags.Solid = true;
        entityFlags.SpawnCeiling = true;
        entityFlags.Special = true;
        entityFlags.Teleport = true;
        entityFlags.Touchy = true;
        entityFlags.WeaponMeleeWeapon = true;
        entityFlags.WeaponNoAlert = true;
        entityFlags.WeaponNoAutofire = true;
        entityFlags.WeaponNoAutoSwitch = true;
        entityFlags.WeaponWimpyWeapon = true;
        entityFlags.WindThrust = true;
        entityFlags.BossSpawnShot = true;


        EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
        EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

        backToEntityFlags.Equals(entityFlags).Should().BeTrue();
    }

    [Fact(DisplayName = "TEntityFlagsModel conversion (alternating true/false)")]
    public void TestAlternatingFlags()
    {
        EntityFlags entityFlags = new EntityFlags();
        entityFlags.ActLikeBridge = true;
        entityFlags.Ambush = false;
        entityFlags.Boss = true;
        entityFlags.Bright = false;
        entityFlags.Corpse = true;
        entityFlags.CountItem = false;
        entityFlags.CountKill = true;
        entityFlags.DoHarmSpecies = false;
        entityFlags.DontFall = true;
        entityFlags.DontGib = false;
        entityFlags.Dropoff = true;
        entityFlags.Dropped = false;
        entityFlags.Float = true;
        entityFlags.ForceRadiusDmg = false;
        entityFlags.Friendly = true;
        entityFlags.FullVolDeath = false;
        entityFlags.Invisible = true;
        entityFlags.Invulnerable = false;
        entityFlags.IsMonster = true;
        entityFlags.IsTeleportSpot = true;
        entityFlags.JustAttacked = false;
        entityFlags.JustHit = true;
        entityFlags.MbfBouncer = false;
        entityFlags.Missile = true;
        entityFlags.MissileEvenMore = false;
        entityFlags.MissileMore = false;
        entityFlags.NoBlockmap = true;
        entityFlags.NoBlood = false;
        entityFlags.NoClip = true;
        entityFlags.NoFriction = false;
        entityFlags.NoGravity = true;
        entityFlags.NoRadiusDmg = false;
        entityFlags.NoSector = true;
        entityFlags.NoTarget = false;
        entityFlags.NotDMatch = true;
        entityFlags.NoTeleport = false;
        entityFlags.NoVerticalMeleeRange = true;
        entityFlags.OldRadiusDmg = false;
        entityFlags.Pickup = true;
        entityFlags.QuickToRetaliate = false;
        entityFlags.Randomize = true;
        entityFlags.Ripper = false;
        entityFlags.Shadow = true;
        entityFlags.Shootable = false;
        entityFlags.Skullfly = true;
        entityFlags.SlidesOnWalls = false;
        entityFlags.Solid = true;
        entityFlags.SpawnCeiling = false;
        entityFlags.Special = false;
        entityFlags.Teleport = true;
        entityFlags.Touchy = false;
        entityFlags.WeaponMeleeWeapon = true;
        entityFlags.WeaponNoAlert = false;
        entityFlags.WeaponNoAutofire = true;
        entityFlags.WeaponNoAutoSwitch = false;
        entityFlags.WeaponWimpyWeapon = true;
        entityFlags.WindThrust = false;
        entityFlags.BossSpawnShot = true;


        EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
        EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

        backToEntityFlags.Equals(entityFlags).Should().BeTrue();
    }
}
