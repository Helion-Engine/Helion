using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using System;
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
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 2);

        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, 92));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(2885.33333, -484, 0)).Should().BeTrue();
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 2);
    }

    [Fact(DisplayName = "Boom Action 263 (WR) teleport line reverse")]
    public void Action263()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, -80));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(3024, -122.66666, 0)).Should().BeTrue();        
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.North), 2);

        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.SetEntityPosition(World, Player, (2864, -168));
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(3112, -122.66666, 0)).Should().BeTrue();
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.North), 2);
    }

    [Fact(DisplayName = "Boom Action 243 (W1) teleport line")]
    public void Action243()
    {
        Player.AngleRadians = GameActions.GetAngle(Bearing.West);
        GameActions.SetEntityPosition(World, Player, (2920, 464));
        GameActions.MoveEntity(World, Player, 32);
        // Should land exactly on the line. Teleport should check if the player's new position is valid.
        // If not then it ignores correctly setting the position to fix jitter to match the original behavior so physics isn't broken.
        Player.Position.Should().Be(new Vec3D(2768, 464, 0));
        MathHelper.GetNormalAngle(Player.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.West), 2);
    }

    [Fact(DisplayName = "Boom Action 266 (W1) monster teleport line")]
    public void Action266()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (2908, 400, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, eye, 32);
        // Should land exactly on the line. The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.Should().Be(new Vec3D(2768, 400, 0));
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 2);
    }

    [Fact(DisplayName = "Boom Action 267 (WR) monster teleport line")]
    public void Action267()
    {
        var eye = GameActions.CreateEntity(World, "EvilEye", (2908, 336, 0), frozen: false);
        eye.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, eye, 32);
        // Should land exactly on the line. The player jitter fix is ignored non-player and player voodoo doll things.
        eye.Position.Should().Be(new Vec3D(2768, 336, 0));
        MathHelper.GetNormalAngle(eye.AngleRadians).Should().BeApproximately(GameActions.GetAngle(Bearing.East), 2);
    }
}