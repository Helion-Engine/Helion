using FluentAssertions;
using Helion.Maps.Specials;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    const string DefaultFloor = "RROCK19";
    const string DefaultCeiling = "SLIME16";

    [Fact(DisplayName = "Generic floor to next floor + transfer special numeric")]
    public void GenericFloorToNextFloorTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 29);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);
        GameActions.ActivateLine(World, Player, 457, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR5_1");
        sector.Floor.Z.Should().Be(16);
    }

    [Fact(DisplayName = "Generic floor to lowest ceiling + transfer special numeric")]
    public void GenericFloorToLowestCeilingTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 30);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);
        GameActions.ActivateLine(World, Player, 484, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Floor, "SLIME13");
        sector.Floor.Z.Should().Be(48);
    }

    [Fact(DisplayName = "Generic ceiling to lowest ceiling + transfer special numeric")]
    public void GenericCeilingToLowestCeilingTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 31);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);
        GameActions.ActivateLine(World, Player, 512, ActivationContext.UseLine).Should().BeTrue();

        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, "RROCK03");
        sector.Ceiling.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Generic ceiling to highest floor + transfer special numeric")]
    public void GenericCeilingToHighestFloorTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 32);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);
        GameActions.ActivateLine(World, Player, 485, ActivationContext.UseLine).Should().BeTrue();

        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, "SLIME13");
        sector.Ceiling.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Generic floor lower 32 + transfer special trigger")]
    public void GenericFloorTransferSpecialTrigger()
    {
        var sector = GameActions.GetSectorByTag(World, 33);
        sector.SectorDamageSpecial.Should().BeNull();
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);
        GameActions.ActivateLine(World, Player, 519, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        sector.SectorDamageSpecial.Should().BeNull();
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);

        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.CheckPlaneTexture(World, sector.Floor, "RROCK01");
        sector.SectorDamageSpecial.Should().NotBeNull();
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "Generic floor crusher")]
    public void GenericFloorCrusher()
    {
        World.ResetGametick();
        var sector = GameActions.GetSectorByTag(World, 34);
        var monster = GameActions.GetSectorEntity(World, sector.Id, "DoomImp");
        monster.Health.Should().Be(60);
        sector.Floor.Z.Should().Be(0);
        GameActions.ActivateLine(World, Player, 527, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        // Floor crush doesn't continue when blocked but still damages
        monster.Health.Should().BeLessThan(60);
        sector.Floor.Z.Should().Be(0);

        GameActions.RunSectorPlaneSpecial(World, sector);
        monster.Health.Should().Be(0);
        sector.Floor.Z.Should().Be(56);
    }

    [Fact(DisplayName = "Generic ceiling crusher")]
    public void GenericCeilingCrusher()
    {
        World.ResetGametick();
        var sector = GameActions.GetSectorByTag(World, 35);
        var monster = GameActions.GetSectorEntity(World, sector.Id, "DoomImp");
        monster.Health.Should().Be(60);
        sector.Ceiling.Z.Should().Be(56);
        GameActions.ActivateLine(World, Player, 552, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        // Ceiling crush doesn't block and does crush damage like vanilla doom but doesn't slow down
        monster.Health.Should().BeLessThan(60);
        sector.Ceiling.Z.Should().Be(55);

        // RunCeilingLower ensures the speed is the same and doesn't slow down when crushing
        GameActions.RunCeilingLower(World, sector, 0, 8);
        monster.Health.Should().Be(0);
        sector.Ceiling.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster can't activate generic floor without numeric model")]
    public void MonsterCantActivateGenericFloor()
    {
        var monsterSector = GameActions.GetSectorByTag(World, 42);
        var monster = GameActions.GetSectorEntity(World, monsterSector.Id, "DoomImp");
        var moveSector = GameActions.GetSectorByTag(World, 44);

        GameActions.ActivateLine(World, monster, 621, ActivationContext.CrossLine).Should().BeFalse();
        moveSector.ActiveFloorMove.Should().BeNull();
    }

    [Fact(DisplayName = "Monster can activate generic floor with numeric model")]
    public void MonsterCanActivateGenericFloor()
    {
        var monsterSector = GameActions.GetSectorByTag(World, 43);
        var monster = GameActions.GetSectorEntity(World, monsterSector.Id, "DoomImp");
        var moveSector = GameActions.GetSectorByTag(World, 45);

        GameActions.ActivateLine(World, monster, 624, ActivationContext.CrossLine).Should().BeTrue();
        moveSector.ActiveFloorMove.Should().NotBeNull();
    }

    [Fact(DisplayName = "Monster can't activate generic ceiling without numeric model")]
    public void MonsterCantActivateGenericCeiling()
    {
        var monsterSector = GameActions.GetSectorByTag(World, 46);
        var monster = GameActions.GetSectorEntity(World, monsterSector.Id, "DoomImp");
        var moveSector = GameActions.GetSectorByTag(World, 48);

        GameActions.ActivateLine(World, monster, 644, ActivationContext.CrossLine).Should().BeFalse();
        moveSector.ActiveCeilingMove.Should().BeNull();
    }

    [Fact(DisplayName = "Monster can activate generic ceiling with numeric model")]
    public void MonsterCanActivateGenericCeiling()
    {
        var monsterSector = GameActions.GetSectorByTag(World, 47);
        var monster = GameActions.GetSectorEntity(World, monsterSector.Id, "DoomImp");
        var moveSector = GameActions.GetSectorByTag(World, 49);

        GameActions.ActivateLine(World, monster, 633, ActivationContext.CrossLine).Should().BeTrue();
        moveSector.ActiveCeilingMove.Should().NotBeNull();
    }
}
