using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Boom Action 207 (W1) silent teleport matching landing angle")]
    public void Action207()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 365, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom Action 208 (WR) silent teleport matching landing angle")]
    public void Action208()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 249, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom Action 208 (WR) silent teleport bad landing angle (+180)")]
    public void SilentTeleportOppositeAngle()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 254, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians + Math.PI);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom Action 208 (WR) silent teleport bad landing angle (+180)")]
    public void SilentTeleportOppositeAngle2()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 255, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians + Math.PI);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom Action 244 (WR) teleport line")]
    public void Action244()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, 176));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(2885.33333, -400, 0)).Should().BeTrue();
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 0.01);

        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, 92));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(2885.33333, -484, 0)).Should().BeTrue();
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 0.01);
    }

    [Fact(DisplayName = "Boom Action 244 (WR) teleport line player voodoo doll")]
    public void Action244VooDooDoll()
    {
        var voodoo = GameActions.GetEntities(World, "DoomPlayer").Single(x => x.PlayerObj != null && x.PlayerObj.IsVooDooDoll);
        voodoo.PlayerObj.Should().NotBeNull();
        voodoo.PlayerObj.IsVooDooDoll.Should().BeTrue();
        GameActions.MoveEntity(World, voodoo, 32);
        // The player jitter fix is ignored non-player and player voodoo doll things.
        voodoo.Position.X.Should().BeApproximately(3106.00016, 0.0001, "Boom fudge");
        voodoo.Position.Y.Should().Be(352);
        voodoo.Position.Z.Should().Be(0);
        MathHelper.GetNormalAngle(voodoo.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 0.01);
    }

    [Fact(DisplayName = "Boom Action 263 (WR) teleport line reverse")]
    public void Action263()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, -80));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(3024, -122.66666, 0)).Should().BeTrue();        
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.North), 0.01);

        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, -168));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(3112, -122.66666, 0)).Should().BeTrue();
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.North), 0.01);
    }

    [Fact(DisplayName = "Boom Action 266 (W1) monster teleport line x")]
    public void Action266X()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (2908, 400, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, eye, 32);
        // The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.X.Should().BeApproximately(2768.00016, 5, "Boom fudge");
        eye.Position.Y.Should().Be(400);
        eye.Position.Z.Should().Be(0);
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 0.01);
    }

    [Fact(DisplayName = "Boom Action 266 (W1) monster teleport line y")]
    public void Action266Y()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (2768, 728, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.MoveEntity(World, eye, 32);
        // The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.X.Should().Be(2768);
        eye.Position.Y.Should().BeApproximately(591.99983, 5, "Boom fudge");
        eye.Position.Z.Should().Be(0);
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.South), 0.01);
    }

    [Fact(DisplayName = "Boom Action 266 (W1) monster teleport line x and y")]
    public void Action266XY()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (3016, 728, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.MoveEntity(World, eye, 32);
        // The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.X.Should().Be(2940);
        eye.Position.Y.Should().BeApproximately(603.00016, 5, "Boom fudge");
        eye.Position.Z.Should().Be(0);
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.North), 0.1);
    }

    [Fact(DisplayName = "Boom Action 267 (WR) monster teleport line")]
    public void Action267()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (2908, 336, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, eye, 32);
        // The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.X.Should().BeApproximately(2768.00016, 5, "Boom fudge");
        eye.Position.Y.Should().Be(336);
        eye.Position.Z.Should().Be(0);
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 0.01);
    }
}