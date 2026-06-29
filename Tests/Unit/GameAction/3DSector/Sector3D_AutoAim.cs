using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_AutoAim : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Sector3D_AutoAim()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-physics.zip", "sector3d-physics.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Auto aim hit to first floor from below")]
    public void AutoAimHitScanToFirstFloorsBelow()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 640, 120));
        GameActions.SetEntityPosition(World, Player, (-1504, 640, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1304, 2);
        data.Intersect.Y.Should().BeApproximately(640, 2);
        data.Intersect.Z.Should().BeApproximately(167.5, 2);

        GameActions.SetEntityPosition(World, Player, (-1376, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1328, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1280, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit to second floor from below")]
    public void AutoAimHitScanToSecondFloorBelow()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 640, 248));
        GameActions.SetEntityPosition(World, Player, (-1504, 640, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1304, 2);
        data.Intersect.Y.Should().BeApproximately(640, 2);
        data.Intersect.Z.Should().BeApproximately(311.49, 2);

        GameActions.SetEntityPosition(World, Player, (-1376, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1328, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1280, 640, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit to first floor from above")]
    public void AutoAimHitScanToFirstFloorAbove()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 640, 120));
        GameActions.SetEntityPosition(World, Player, (-1504, 640, 256));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1304, 2);
        data.Intersect.Y.Should().BeApproximately(640, 2);
        data.Intersect.Z.Should().BeApproximately(152.58, 2);

        GameActions.SetEntityPosition(World, Player, (-1376, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1304, 2);
        data.Intersect.Y.Should().BeApproximately(640, 2);
        data.Intersect.Z.Should().BeApproximately(147.5, 2);

        GameActions.SetEntityPosition(World, Player, (-1328, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1280, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
      
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit below first floor from above")]
    public void AutoAimHitScanBelowFirstFloorAbove()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 640, 0));
        GameActions.SetEntityPosition(World, Player, (-1504, 640, 256));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1304, 2);
        data.Intersect.Y.Should().BeApproximately(640, 2);
        data.Intersect.Z.Should().BeApproximately(32, 2);

        GameActions.SetEntityPosition(World, Player, (-1376, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1328, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();

        GameActions.SetEntityPosition(World, Player, (-1280, 640, 256));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit scan not blocked by inverted shoot flag from below")]
    public void AutoAimHitScanThroughInvertedShootSectorsToSecondFloor()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 688, 256));
        GameActions.SetEntityPosition(World, Player, (-1280, 896, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1280, 2);
        data.Intersect.Y.Should().BeApproximately(712, 2);
        data.Intersect.Z.Should().BeApproximately(284, 2);

        // Bar blocks in combination with second floor
        GameActions.SetEntityPosition(World, Player, (-1280, 904, 0));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit scan not blocked by inverted shoot flag from first floor")]
    public void AutoAimHitScanThroughInvertedShootSectorsFromFirstFloor()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1280, 912, 0));
        GameActions.SetEntityPosition(World, Player, (-1280, 640, 120));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);

        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1280, 2);
        data.Intersect.Y.Should().BeApproximately(888, 2);
        data.Intersect.Z.Should().BeApproximately(58.78, 2);

        GameActions.SetEntityPosition(World, Player, (-1280, 592, 128));
        data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Auto aim hit scan not blocked by inverted shoot flag from below")]
    public void AutoAimHitScanThroughInvertedShootSectorsBelow()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1312, 848, 72));
        GameActions.SetEntityPosition(World, Player, (-1424, 848, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1336, 2);
        data.Intersect.Y.Should().BeApproximately(848, 2);
        data.Intersect.Z.Should().BeApproximately(104, 2);
    }

    [Fact(DisplayName = "Auto aim hit scan picks larger visible span")]
    public void AutoAimHitScanThroughLargerVisibleSpan()
    {
        // Splits between two visible spans. The bottom span is very small while the top is large.
        // Verifies that the larger span is picked
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-1312, 848, 72));
        GameActions.SetEntityPosition(World, Player, (-1616, 848, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        var data = GameActions.FireHitScanAutoAimTest(World, Player);
        data.HitEntity.Should().Be(baron);

        data.Intersect.X.Should().BeApproximately(-1336, 2);
        data.Intersect.Y.Should().BeApproximately(848, 2);
        data.Intersect.Z.Should().BeApproximately(115.61, 2);
    }
}
