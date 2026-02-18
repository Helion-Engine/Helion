using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_HitScan : IDisposable
{
    private readonly SinglePlayerWorld World;
    private readonly Entity Test;
    private Player Player => World.Player;

    public Sector3D_HitScan()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-physics.zip", "sector3d-physics.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        Test = GameActions.CreateEntity(World, "Column", default, frozen: false);
        Test.Height = 56;
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Hit scan hits normal wall")]
    public void HitScanWallNormal()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        Player.PitchRadians = 0;
        GameActions.SetEntityPosition(World, Player, (736, -128, 0));
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(140);

        data.Intersect.X.Should().BeApproximately(862, 2);
        data.Intersect.Y.Should().BeApproximately(-128, 2);
        data.Intersect.Z.Should().BeApproximately(36, 2);
    }

    [Fact(DisplayName = "Hit scan hits floor plane with line crossed")]
    public void HitScanFloorLineCross()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        Player.PitchRadians = MathHelper.ToRadians(-30);
        GameActions.SetEntityPosition(World, Player, (736, -128, 0));
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Id.Should().Be(42);

        data.Intersect.X.Should().BeApproximately(798.35, 2);
        data.Intersect.Y.Should().BeApproximately(-128, 2);
        data.Intersect.Z.Should().BeApproximately(0, 2);
    }

    [Fact(DisplayName = "Hit scan hits ceiling plane with line crossed")]
    public void HitScanCeilingLineCross()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        Player.PitchRadians = MathHelper.ToRadians(40);
        GameActions.SetEntityPosition(World, Player, (736, -128, 0));
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Id.Should().Be(42);

        data.Intersect.X.Should().BeApproximately(845.64, 2);
        data.Intersect.Y.Should().BeApproximately(-128, 2);
        data.Intersect.Z.Should().BeApproximately(124, 2);
    }

    [Fact(DisplayName = "Hit scan hits floor plane with no lines crossed")]
    public void HitScanFloorNoLineCross()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        Player.PitchRadians = MathHelper.ToRadians(-60);
        GameActions.SetEntityPosition(World, Player, (320, -128, 0));
        var data = GameActions.FireHitScanTest(World, Player, 512);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Id.Should().Be(42);
        data.HitLine.Should().BeNull();

        data.Intersect.X.Should().BeApproximately(340.78, 2);
        data.Intersect.Y.Should().BeApproximately(-128, 2);
        data.Intersect.Z.Should().BeApproximately(0, 2);
    }

    [Fact(DisplayName = "Hit scan hits ceiling plane with no lines crossed")]
    public void HitScanCeilingNoLineCross()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        Player.PitchRadians = MathHelper.ToRadians(60);
        GameActions.SetEntityPosition(World, Player, (320, -128, 0));
        var data = GameActions.FireHitScanTest(World, Player, 512);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Id.Should().Be(42);
        data.HitLine.Should().BeNull();

        data.Intersect.X.Should().BeApproximately(373.11, 2);
        data.Intersect.Y.Should().BeApproximately(-128, 2);
        data.Intersect.Z.Should().BeApproximately(124, 2);
    }

    [Fact(DisplayName = "Hit scan hits 3D sector wall")]
    public void HitScanWall3D()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, (-576, 160, 0));

        // below lower 3D sector
        Player.PitchRadians = MathHelper.ToRadians(0);
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(0);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(-254, 2);
        data.Intersect.Z.Should().BeApproximately(36, 2);

        // lower 3D sector
        Player.PitchRadians = MathHelper.ToRadians(12);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(2);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(0, 2);
        data.Intersect.Z.Should().BeApproximately(70.76, 2);

        // between lower and middle 3D sector
        Player.PitchRadians = MathHelper.ToRadians(21);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(0);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(-254, 2);
        data.Intersect.Z.Should().BeApproximately(195.68, 2);

        // middle 3D sector
        Player.PitchRadians = MathHelper.ToRadians(40);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(3);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(0, 2);
        data.Intersect.Z.Should().BeApproximately(166.25, 2);

        // top 3D sector
        Player.PitchRadians = MathHelper.ToRadians(52);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(4);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(0, 2);
        data.Intersect.Z.Should().BeApproximately(240.79, 2);

        // above top 3D sector
        Player.PitchRadians = MathHelper.ToRadians(56);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(0);
        data.Intersect.X.Should().BeApproximately(-576, 2);
        data.Intersect.Y.Should().BeApproximately(-254, 2);
        data.Intersect.Z.Should().BeApproximately(652.74, 2);
    }

    [Fact(DisplayName = "Hit scan hits 3D sector wall crossing multiple 3D sector lines")]
    public void HitScanSectorMultiPass3D()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.West);
        GameActions.SetEntityPosition(World, Player, (240, -64, 0));

        // pass through bottom and hit's farther stair sector
        Player.PitchRadians = MathHelper.ToRadians(0);
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(12);

        // middle 3D sector
        Player.PitchRadians = MathHelper.ToRadians(30);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(3);

        // pass through and hits normal wall
        Player.PitchRadians = MathHelper.ToRadians(34);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(1);

        // top 3D sector
        Player.PitchRadians = MathHelper.ToRadians(42);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().NotBeNull();
        data.HitSector.Sector3D.ControlSector.Id.Should().Be(4);

        // over top 3D sector
        Player.PitchRadians = MathHelper.ToRadians(56);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitSector.Should().NotBeNull();
        data.HitSector.Sector3D.Should().BeNull();
        data.HitSector.Id.Should().Be(6);
    }

    [Fact(DisplayName = "Hit scan blocked by top doesn't hit entity")]
    public void TopBlocksEntityHit()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-576, -32, 96));
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, (-576, 160, 245));

        Player.PitchRadians = MathHelper.ToRadians(-31);
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().BeNull();

        Player.PitchRadians = MathHelper.ToRadians(-36);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().BeNull();

        Player.PitchRadians = MathHelper.ToRadians(-40);
        data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().Be(baron);
    }

    [Fact(DisplayName = "Hit scan blocked by bottom doesn't hit entity")]
    public void CeilingBlocksEntityHit()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-576, -32, 96));
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, (-576, 10, 0));

        Player.PitchRadians = MathHelper.ToRadians(44);
        var data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-576, 43, 0));
        data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-576, 76, 0));
        data = GameActions.FireHitScanTest(World, Player);
        data.HitEntity.Should().Be(baron);
    }

    [Fact(DisplayName = "Hit scan not blocked by non-solid sector 3D")]
    public void HitScanNotBlockedByNonSolid()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Player, (512, 288, 0));
        Player.PitchRadians = 0;

        var data = GameActions.FireHitScanTest(World, Player);
        data.HitLine.Should().NotBeNull();
        data.HitLine.Id.Should().Be(2);
    }
}
