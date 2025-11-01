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
        entityFlags.SetActLikeBridge();
        entityFlags.SetAmbush();
        entityFlags.SetBoss();
        entityFlags.SetBright();
        entityFlags.SetCorpse();
        entityFlags.SetCountItem();
        entityFlags.SetCountKill();
        entityFlags.SetDoHarmSpecies();
        entityFlags.SetDontFall();
        entityFlags.SetDontGib();
        entityFlags.SetDropoff();
        entityFlags.SetDropped();
        entityFlags.SetFloat();
        entityFlags.SetForceRadiusDmg();
        entityFlags.SetFriendly();
        entityFlags.SetFullVolDeath();       
        entityFlags.SetInvisible();
        entityFlags.SetInvulnerable();
        entityFlags.SetIsMonster();
        entityFlags.SetIsTeleportSpot();
        entityFlags.SetJustAttacked();
        entityFlags.SetJustHit();
        entityFlags.SetMbfBouncer();
        entityFlags.SetMissile();
        entityFlags.SetMissileEvenMore();
        entityFlags.SetMissileMore();        
        entityFlags.SetNoBlockmap();
        entityFlags.SetNoBlood();
        entityFlags.SetNoClip();
        entityFlags.SetNoFriction();
        entityFlags.SetNoGravity();
        entityFlags.SetNoRadiusDmg();
        entityFlags.SetNoSector();
        entityFlags.SetNoTarget();
        entityFlags.SetNotDMatch();
        entityFlags.SetNoTeleport();
        entityFlags.SetNoVerticalMeleeRange();
        entityFlags.SetOldRadiusDmg();
        entityFlags.SetPickup();
        entityFlags.SetQuickToRetaliate();
        entityFlags.SetRandomize();
        entityFlags.SetRipper();
        entityFlags.SetShadow();
        entityFlags.SetShootable();
        entityFlags.SetSkullfly();
        entityFlags.SetSlidesOnWalls();
        entityFlags.SetSolid();
        entityFlags.SetSpawnCeiling();
        entityFlags.SetSpecial();
        entityFlags.SetTeleport();
        entityFlags.SetTouchy();
        entityFlags.SetWeaponMeleeWeapon();
        entityFlags.SetWeaponNoAlert();
        entityFlags.SetWeaponNoAutofire();
        entityFlags.SetWeaponNoAutoSwitch();
        entityFlags.SetWeaponWimpyWeapon();
        entityFlags.SetWindThrust();
        entityFlags.SetBossSpawnShot();


        EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
        EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

        backToEntityFlags.Equals(entityFlags).Should().BeTrue();
    }

    [Fact(DisplayName = "TEntityFlagsModel conversion (alternating true/false)")]
    public void TestAlternatingFlags()
    {
        EntityFlags entityFlags = new EntityFlags();
        entityFlags.SetActLikeBridge();
        entityFlags.ClearAmbush();
        entityFlags.SetBoss();
        entityFlags.ClearBright();
        entityFlags.SetCorpse();
        entityFlags.ClearCountItem();
        entityFlags.SetCountKill();
        entityFlags.ClearDoHarmSpecies();
        entityFlags.SetDontFall();
        entityFlags.ClearDontGib();
        entityFlags.SetDropoff();
        entityFlags.ClearDropped();
        entityFlags.SetFloat();
        entityFlags.ClearForceRadiusDmg();
        entityFlags.SetFriendly();
        entityFlags.ClearFullVolDeath();
        entityFlags.SetInvisible();
        entityFlags.ClearInvulnerable();
        entityFlags.SetIsMonster();
        entityFlags.SetIsTeleportSpot();
        entityFlags.ClearJustAttacked();
        entityFlags.SetJustHit();
        entityFlags.ClearMbfBouncer();
        entityFlags.SetMissile();
        entityFlags.ClearMissileEvenMore();
        entityFlags.ClearMissileMore();
        entityFlags.SetNoBlockmap();
        entityFlags.ClearNoBlood();
        entityFlags.SetNoClip();
        entityFlags.ClearNoFriction();
        entityFlags.SetNoGravity();
        entityFlags.ClearNoRadiusDmg();
        entityFlags.SetNoSector();
        entityFlags.ClearNoTarget();
        entityFlags.SetNotDMatch();
        entityFlags.ClearNoTeleport();
        entityFlags.SetNoVerticalMeleeRange();
        entityFlags.ClearOldRadiusDmg();
        entityFlags.SetPickup();
        entityFlags.ClearQuickToRetaliate();
        entityFlags.SetRandomize();
        entityFlags.ClearRipper();
        entityFlags.SetShadow();
        entityFlags.ClearShootable();
        entityFlags.SetSkullfly();
        entityFlags.ClearSlidesOnWalls();
        entityFlags.SetSolid();
        entityFlags.ClearSpawnCeiling();
        entityFlags.ClearSpecial();
        entityFlags.ClearTeleport();
        entityFlags.ClearTouchy();
        entityFlags.SetWeaponMeleeWeapon();
        entityFlags.ClearWeaponNoAlert();
        entityFlags.SetWeaponNoAutofire();
        entityFlags.ClearWeaponNoAutoSwitch();
        entityFlags.SetWeaponWimpyWeapon();
        entityFlags.ClearWindThrust();
        entityFlags.SetBossSpawnShot();


        EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
        EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

        backToEntityFlags.Equals(entityFlags).Should().BeTrue();
    }
}
