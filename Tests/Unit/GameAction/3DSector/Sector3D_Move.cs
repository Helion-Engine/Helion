using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Move
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Sector3D_Move()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-move.zip", "sector3d-move.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
        World.TextureManager.SetSkyTexture("TEST");
    }

    [Fact(DisplayName = "3D sector floor lower")]
    public void FloorLower()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.ActivateLine(World, Player, 11, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunFloorLower(World, sector3D.ControlSector, 0, 16);

        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector ceiling raise")]
    public void CeilingRaise()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.ActivateLine(World, Player, 23, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunCeilingRaise(World, sector3D.ControlSector, 256, 16);

        sector3D.ControlTop.Z.Should().Be(256);
        sector3D.ControlBottom.Z.Should().Be(128);
    }

    [Fact(DisplayName = "3D sector platform lower/raise")]
    public void PlatformLowerRaise()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);

        sector3D.ControlTop.Z.Should().Be(16);
        sector3D.ControlBottom.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 53, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);

        sector3D.ControlTop.Z.Should().Be(128);
        sector3D.ControlBottom.Z.Should().Be(112);
    }

    [Fact(DisplayName = "3D sector platform lower blocked by entity")]
    public void PlatformLowerBlock()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        var imp = GameActions.CreateEntity(World, "DoomImp", (-80, 768, 0));

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return sector3D.ControlBottom.Z != 56; }, () => { });

        sector3D.ControlTop.Z.Should().Be(72);
        sector3D.ControlBottom.Z.Should().Be(56);

        GameActions.TickWorld(World, 1);

        // Still blocked
        sector3D.ControlTop.Z.Should().Be(72);
        sector3D.ControlBottom.Z.Should().Be(56);

        var entity = imp.LowestCeilingEntity();
        entity.Should().NotBeNull();
        entity.Sector3D.Should().NotBeNull();
        entity.Sector3D.Should().Be(sector3D);

        imp.Kill(null);

        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);
        sector3D.ControlTop.Z.Should().Be(16);
        sector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector platform raise blocked by entity")]
    public void PlatformRaiseBlock()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        var imp = GameActions.CreateEntity(World, "DoomImp", (-80, 768, 144));

        GameActions.ActivateLine(World, Player, 53, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return sector3D.ControlTop.Z != 200; }, () => { });

        sector3D.ControlTop.Z.Should().Be(200);
        sector3D.ControlBottom.Z.Should().Be(184);

        GameActions.TickWorld(World, 1);

        // Still blocked
        sector3D.ControlTop.Z.Should().Be(200);
        sector3D.ControlBottom.Z.Should().Be(184);

        var entity = imp.HighestFloorEntity();
        entity.Should().NotBeNull();
        entity.Sector3D.Should().NotBeNull();
        entity.Sector3D.Should().Be(sector3D);

        imp.Kill(null);

        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);
        sector3D.ControlTop.Z.Should().Be(256);
        sector3D.ControlBottom.Z.Should().Be(240);
    }

    [Fact(DisplayName = "Multiple 3D sector platforms lower causes all to be blocked")]
    public void MultiPlatformLowerBlock()
    {
        var sectors = GameActions.GetSectorsByTag(World, 7);
        sectors.Count.Should().Be(2);
        sectors[0].Sectors3D.Length.Should().Be(1);
        sectors[1].Sectors3D.Length.Should().Be(1);

        var sector3D = sectors[0].Sectors3D[0];
        var secondSector3D = sectors[1].Sectors3D[0];
        sector3D.ControlSector.Should().Be(secondSector3D.ControlSector);

        var imp = GameActions.CreateEntity(World, "DoomImp", (160, 800, 0));

        GameActions.ActivateLine(World, Player, 73, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return sector3D.ControlBottom.Z != 56; }, () => { });

        sector3D.ControlTop.Z.Should().Be(72);
        sector3D.ControlBottom.Z.Should().Be(56);
        secondSector3D.ControlTop.Z.Should().Be(72);
        secondSector3D.ControlBottom.Z.Should().Be(56);

        GameActions.TickWorld(World, 1);

        // Still blocked
        sector3D.ControlTop.Z.Should().Be(72);
        sector3D.ControlBottom.Z.Should().Be(56);
        secondSector3D.ControlTop.Z.Should().Be(72);
        secondSector3D.ControlBottom.Z.Should().Be(56);

        imp.Kill(null);

        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);
        sector3D.ControlTop.Z.Should().Be(16);
        sector3D.ControlBottom.Z.Should().Be(0);
        secondSector3D.ControlTop.Z.Should().Be(16);
        secondSector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector non solid platform lower not blocked by entity")]
    public void NonSolidPlatformNotBlocked()
    {
        var sector = GameActions.GetSectorByTag(World, 9);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        var imp = GameActions.CreateEntity(World, "DoomImp", (512, 768, 0));
        imp.Sector.Should().Be(sector);

        GameActions.ActivateLine(World, Player, 88, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector);

        sector3D.ControlTop.Z.Should().Be(16);
        sector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector platform blocked with entity between 3D sector platforms")]
    public void PlatformLowerBlockInBetween()
    {
        var sector = GameActions.GetSectorByTag(World, 11);
        sector.Sectors3D.Length.Should().Be(2);

        var topSector3D = sector.Sectors3D[0];
        topSector3D.ControlTop.Z.Should().Be(200);
        topSector3D.ControlBottom.Z.Should().Be(184);

        var bottomSector3D = sector.Sectors3D[1];
        bottomSector3D.ControlTop.Z.Should().Be(80);
        bottomSector3D.ControlBottom.Z.Should().Be(64);

        var imp = GameActions.CreateEntity(World, "DoomImp", (-592, 384, 80));
        imp.Sector.Should().Be(sector);

        GameActions.ActivateLine(World, Player, 120, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return topSector3D.ControlBottom.Z != 136; }, () => { });

        topSector3D.ControlTop.Z.Should().Be(152);
        topSector3D.ControlBottom.Z.Should().Be(136);

        GameActions.TickWorld(World, 1);

        // Still blocked
        topSector3D.ControlTop.Z.Should().Be(152);
        topSector3D.ControlBottom.Z.Should().Be(136);

        var entity = imp.LowestCeilingEntity();
        entity.Should().NotBeNull();
        entity.Sector3D.Should().NotBeNull();
        entity.Sector3D.Should().Be(topSector3D);

        entity = imp.HighestFloorEntity();
        entity.Should().NotBeNull();
        entity.Sector3D.Should().NotBeNull();
        entity.Sector3D.Should().Be(bottomSector3D);

        imp.Kill(null);

        GameActions.RunSectorPlaneSpecial(World, topSector3D.ControlSector);
        topSector3D.ControlTop.Z.Should().Be(16);
        topSector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector solid platform lower passes through another solid platform")]
    public void SolidPlatformPassesThrough()
    {
        var sector = GameActions.GetSectorByTag(World, 11);
        sector.Sectors3D.Length.Should().Be(2);

        var topSector3D = sector.Sectors3D[0];
        topSector3D.ControlTop.Z.Should().Be(200);
        topSector3D.ControlBottom.Z.Should().Be(184);

        var bottomSector3D = sector.Sectors3D[1];
        bottomSector3D.ControlTop.Z.Should().Be(80);
        bottomSector3D.ControlBottom.Z.Should().Be(64);

        GameActions.ActivateLine(World, Player, 120, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, topSector3D.ControlSector);

        topSector3D.ControlTop.Z.Should().Be(16);
        topSector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "3D sector non-solid platform lower passes through another solid platform")]
    public void NonSolidPlatformPassesThrough()
    {    
        var sector = GameActions.GetSectorByTag(World, 14);
        sector.Sectors3D.Length.Should().Be(2);

        var topSector3D = sector.Sectors3D[0];
        topSector3D.ControlTop.Z.Should().Be(208);
        topSector3D.ControlBottom.Z.Should().Be(192);

        var bottomSector3D = sector.Sectors3D[1];
        bottomSector3D.ControlTop.Z.Should().Be(80);
        bottomSector3D.ControlBottom.Z.Should().Be(64);

        GameActions.ActivateLine(World, Player, 146, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, topSector3D.ControlSector);

        topSector3D.ControlTop.Z.Should().Be(16);
        topSector3D.ControlBottom.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Entity sticks to 3D sector platform")]
    public void EntitySticksToPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.SetEntityPosition(World, Player, (-80, 768, 144));
        Player.OnGround.Should().BeTrue();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();

        GameActions.RunSectorPlaneSpecial(World, sector3D.ControlSector, () =>
        {
            Player.Position.Z.Should().Be(sector3D.ControlTop.Z);
            Player.OnGround.Should().BeTrue();
        });
    }

    [Fact(DisplayName = "Player moves on 3D sector moving platform")]
    public void PlayerMovesOnMovingPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.SetEntityPosition(World, Player, (-80, 768, 144));
        Player.OnGround.Should().BeTrue();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();

        var gameTick = World.Gametick + 1;
        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return World.Gametick < gameTick; }, onTick: () =>
        {
            Player.OnGround.Should().BeTrue();
            Player.Position.Z.Should().Be(142);
            Player.Position.Y.Should().Be(769.5625);
        });
    }

    [Fact(DisplayName = "Monster moves on 3D sector moving platform")]
    public void MonsterMovesOnMovingPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);
                
        var imp = GameActions.CreateEntity(World, "DoomImp", (-80, 768, 144), frozen: false);
        imp.OnGround.Should().BeTrue();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1, () =>
        {
            imp.OnGround.Should().BeTrue();
            imp.SetMoveDirection(Helion.World.Entities.Entity.MoveDir.North);
            imp.MoveEnemy(out _).Should().BeTrue();
            imp.Position.Z.Should().Be(142);
            imp.Position.Y.Should().Be(776);
        });
    }

    [Fact(DisplayName = "Player jumps on 3D sector moving platform")]
    public void PlayerJumpsOnMovingPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
        sector3D.ControlTop.Z.Should().Be(144);
        sector3D.ControlBottom.Z.Should().Be(128);

        GameActions.SetEntityPosition(World, Player, (-80, 768, 144));
        Player.OnGround.Should().BeTrue();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1, () =>
        {
            Player.OnGround.Should().BeTrue();
            Player.Position.Z.Should().Be(142);
            Player.Jump();
            Player.Velocity.Z.Should().Be(8);
        });

        GameActions.TickWorld(World, 1);
        Player.OnGround.Should().BeFalse();
        Player.Position.Z.Should().Be(150);
        Player.Velocity.Z.Should().Be(8);
    }

    [Fact(DisplayName = "Missile hits 3D sector control top")]
    public void MissileHitsControlTop()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        GameActions.SetEntityPosition(World, Player, (-80, 592, 172));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(-22);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return plasma.BlockingSectorPlane == null; }, () => { });

        plasma.IsBlocked().Should().BeTrue();
        plasma.Position.Z.Should().Be(144);
        plasma.BlockingSectorPlane.Should().Be(sector3D.ControlTop);
    }

    [Fact(DisplayName = "Missile hits 3D sector control bottom")]
    public void MissileHitsControlBottom()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        GameActions.SetEntityPosition(World, Player, (-80, 592, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(24);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return plasma.BlockingSectorPlane == null; }, () => { });

        plasma.IsBlocked().Should().BeTrue();
        plasma.Position.Z.Should().Be(120);
        plasma.BlockingSectorPlane.Should().Be(sector3D.ControlBottom);
    }

    [Fact(DisplayName = "Missile hits 3D sector control middle")]
    public void MissileHitsCenter()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];
;
        GameActions.SetEntityPosition(World, Player, (-80, 592, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(47);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return plasma.BlockingBlockLineIndex == -1; }, () => { });

        plasma.IsBlocked().Should().BeTrue();
        var line = World.Blockmap.BlockLines[plasma.BlockingBlockLineIndex];
        line.LineId.Should().Be(34);
        plasma.BlockingSector3D.Should().Be(sector3D);
    }

    [Fact(DisplayName = "Missile passes on lowering platform")]
    public void MissilesPassesOnLoweringPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        GameActions.SetEntityPosition(World, Player, (-80, 688, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(0);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();

        GameActions.ActivateLine(World, Player, 53, ActivationContext.UseLine).Should().BeTrue();
        sector3D.ControlSector.ActiveFloorMove.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return plasma.BlockingBlockLineIndex == -1; }, () => { });
        var line = World.Blockmap.BlockLines[plasma.BlockingBlockLineIndex];
        line.LineId.Should().Be(2);
    }

    [Fact(DisplayName = "Missile passes on raising platform")]
    public void MissilesPassesOnRaisingPlatform()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        GameActions.SetEntityPosition(World, Player, (-80, 688, 192));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(0);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        sector3D.ControlSector.ActiveFloorMove.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return plasma.BlockingBlockLineIndex == -1; }, () => { });
        var line = World.Blockmap.BlockLines[plasma.BlockingBlockLineIndex];
        line.LineId.Should().Be(2);
    }

    [Fact(DisplayName = "Moving 3D sector control top destroys missiles")]
    public void ControlTopDestroysMissile()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        GameActions.SetEntityPosition(World, Player, (-80, 688, 112));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(0);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        plasma.Flags.Missile().Should().BeTrue();
        plasma.Velocity = Vec3D.Zero;

        GameActions.ActivateLine(World, Player, 53, ActivationContext.UseLine).Should().BeTrue();
        sector3D.ControlSector.ActiveCeilingMove.Should().NotBeNull();

        World.PhysicsManager.MoveSectorZ(16, 256, sector3D.ControlSector.ActiveCeilingMove, sector3D.ControlSector.ActiveCeilingMove.Sector);

        plasma.BlockingSectorPlane.Should().Be(sector3D.ControlTop);
        plasma.BlockingSector3D.Should().Be(sector3D);
        plasma.Flags.Missile().Should().BeFalse();
    }

    [Fact(DisplayName = "Moving 3D sector control bottom destroys missiles")]
    public void ControlBottomDestroysMissile()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        GameActions.SetEntityPosition(World, Player, (-80, 688, 80));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(0);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        plasma.Flags.Missile().Should().BeTrue();
        plasma.Velocity = Vec3D.Zero;

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        sector3D.ControlSector.ActiveFloorMove.Should().NotBeNull();

        World.PhysicsManager.MoveSectorZ(-16, 0, sector3D.ControlSector.ActiveFloorMove, sector3D.ControlSector.ActiveFloorMove.Sector);

        plasma.BlockingSectorPlane.Should().Be(sector3D.ControlBottom);
        plasma.BlockingSector3D.Should().Be(sector3D);
        plasma.Flags.Missile().Should().BeFalse();
    }
}
