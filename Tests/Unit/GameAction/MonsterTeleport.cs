using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MonsterTeleport
{
    private readonly SinglePlayerWorld World;
    private static readonly Vec2D LowerTelportPos = new(864, 32);
    private static readonly Vec2D UpperTelportPos = new(-928, 32);
    private static readonly Vec2D SilentTeleportPos = new(32, 576);
    const int LowerTeleportLineId = 13;
    const int UpperTeleportLineId = 12;
    const int SilentTeleportLineId = 26;

    const int TeleportLandingSectorId = 0;
    const int LowerTeleportSectorId = 1;
    const int UpperTeleportSectorId = 2;
    const int SilentTeleportSectorId = 4;

    public MonsterTeleport()
    {
        World = WorldAllocator.LoadMap("Resources/monsterteleport.zip", "monsterteleport.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Monster teleport from lower sector")]
    public void MonsterTeleportFromLowerSector()
    {
        var blocker = CreateBlockingMonster(0);
        var monster = CreateMonster(LowerTelportPos);
        ActivateTeleportLine(monster, LowerTeleportLineId);
        monster.Sector.Id.Should().Be(LowerTeleportSectorId);

        blocker.Kill(null);

        ActivateTeleportLine(monster, LowerTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(0);
    }


    [Fact(DisplayName = "Monster teleport from upper sector")]
    public void MonsterTeleportFromUpperSector()
    {
        var blocker = CreateBlockingMonster(0);
        var monster = CreateMonster(UpperTelportPos);
        ActivateTeleportLine(monster, UpperTeleportLineId);
        monster.Sector.Id.Should().Be(UpperTeleportSectorId);

        blocker.Kill(null);

        ActivateTeleportLine(monster, UpperTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster teleport not blocked by z")]
    public void MonsterTeleportNotBlockedByZ()
    {
        CreateBlockingMonster(64);
        var monster = CreateMonster(LowerTelportPos);
        ActivateTeleportLine(monster, LowerTeleportLineId);

        ActivateTeleportLine(monster, LowerTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster teleport silent (keeps z difference)")]
    public void MonsterTeleportSilent()
    {
        var blocker = CreateBlockingMonster(0);
        var monster = CreateMonster(SilentTeleportPos);
        ActivateTeleportLine(monster, SilentTeleportLineId);
        monster.Sector.Id.Should().Be(SilentTeleportSectorId);

        blocker.Kill(null);

        ActivateTeleportLine(monster, SilentTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster teleport silent blocks with z-offset (keeps z difference)")]
    public void MonsterTeleportSilentBlocksOffsetZ()
    {
        var blocker = CreateBlockingMonster(0);
        var monster = CreateMonster(SilentTeleportPos);
        monster.Position.Z = 32;
        ActivateTeleportLine(monster, SilentTeleportLineId);
        monster.Sector.Id.Should().Be(SilentTeleportSectorId);

        blocker.Kill(null);

        ActivateTeleportLine(monster, SilentTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(32);
    }

    [Fact(DisplayName = "Monster teleport silent doesn't block with z-offset (keeps z difference)")]
    public void MonsterTeleportSilentNoBlockOffsetZ()
    {
        CreateBlockingMonster(0);
        var monster = CreateMonster(SilentTeleportPos);
        monster.Position.Z = 56;

        ActivateTeleportLine(monster, SilentTeleportLineId);
        monster.Sector.Id.Should().Be(TeleportLandingSectorId);
        monster.Position.Z.Should().Be(56);
    }

    private Entity CreateMonster(Vec2D pos)
    {
        return GameActions.CreateEntity(World, "DoomImp", pos.To3D(0), frozen: false, initSpawn: true);
    }

    private Entity CreateBlockingMonster(double z)
    {
        return GameActions.CreateEntity(World, "Cacodemon", (32, 32, z));
    }

    private void ActivateTeleportLine(Entity entity, int lineId)
    {
        GameActions.ActivateLine(World, entity, lineId, ActivationContext.CrossLine).Should();
    }
}
