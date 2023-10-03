using FluentAssertions;
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
}