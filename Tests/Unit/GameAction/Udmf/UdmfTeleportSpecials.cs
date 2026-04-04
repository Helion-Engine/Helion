using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfTeleportSpecials
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfTeleportSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmfteleportspecials.zip", "udmfteleportspecials.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Teleport in sector without tid")]
    public void TeleportInSectorWithoutTid()
    {
        var eye = GameActions.GetEntity(World, "EvilEye");
        var col = GameActions.GetEntity(World, "Column");
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        var chain = GameActions.GetEntity(World, "ChaingunGuy");
        var shot = GameActions.GetEntity(World, "ShotgunGuy");
        var knight = GameActions.GetEntity(World, "HellKnight");
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine);
        AssertPosAngle(eye, (-263.76, -11.64, 0), 2.35);
        AssertPosAngle(col, (-263.76, 124.11, 0), 2.35);
        AssertPosAngle(zombie, (-195.88, 192, 0), 2.35);
        AssertPosAngle(imp, (-60.11, 192, 0), 2.35);
        AssertPosAngle(chain, (-128, 192, 0), 2.35);
        AssertPosAngle(shot, (224, 384, 0), 2.35);
        AssertPosAngle(knight, (288, 448, 0), 2.35);
    }

    [Fact(DisplayName = "Teleport in sector with tid")]
    public void TeleportInSectorWithTid()
    {
        var eye = GameActions.GetEntity(World, "EvilEye");
        var col = GameActions.GetEntity(World, "Column");
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        var chain = GameActions.GetEntity(World, "ChaingunGuy");
        var shot = GameActions.GetEntity(World, "ShotgunGuy");
        var knight = GameActions.GetEntity(World, "HellKnight");
        GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine);
        AssertPosAngle(eye, (-263.76, -11.64, 0), 2.35);
        AssertPosAngle(col, (-80, 464, 0), 2.35);
        AssertPosAngle(zombie, (16, 464, 0), 2.35);
        AssertPosAngle(imp, (-60.11, 192, 0), 2.35);
        AssertPosAngle(chain, (64, 416, 0), 2.35);
        AssertPosAngle(shot, (224, 384, 0), 2.35);
        AssertPosAngle(knight, (7.764, 373.01, 0), 2.35);
    }

    [Fact(DisplayName = "Teleport group")]
    public void TeleportGroup()
    {
        var pos = Player.Position;
        var eye = GameActions.GetEntity(World, "EvilEye");
        var col = GameActions.GetEntity(World, "Column");
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        var chain = GameActions.GetEntity(World, "ChaingunGuy");
        var shot = GameActions.GetEntity(World, "ShotgunGuy");
        var knight = GameActions.GetEntity(World, "HellKnight");
        GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine);
        AssertPosAngle(eye, (-263.76, -11.64, 0), 2.35);
        AssertPosAngle(col, (-80, 464, 0), 2.35);
        AssertPosAngle(zombie, (16, 464, 0), 2.35);
        AssertPosAngle(imp, (-60.11, 192, 0), 2.35);
        AssertPosAngle(chain, (64, 416, 0), 2.35);
        AssertPosAngle(shot, (224, 384, 0), 2.35);
        AssertPosAngle(knight, (7.764, 373.01, 0), 2.35);
        AssertPosAngle(Player, pos, Player.AngleRadians);
    }

    [Fact(DisplayName = "Teleport group move source")]
    public void TeleportGroupMoveSource()
    {
        var pos = Player.Position;
        var eye = GameActions.GetEntity(World, "EvilEye");
        var col = GameActions.GetEntity(World, "Column");
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        var chain = GameActions.GetEntity(World, "ChaingunGuy");
        var shot = GameActions.GetEntity(World, "ShotgunGuy");
        var knight = GameActions.GetEntity(World, "HellKnight");
        GameActions.ActivateLine(World, Player, 28, ActivationContext.UseLine);
        AssertPosAngle(eye, (-263.76, -11.64, 0), 2.35);
        AssertPosAngle(col, (-80, 464, 0), 2.35);
        AssertPosAngle(zombie, (16, 464, 0), 2.35);
        AssertPosAngle(imp, (-60.11, 192, 0), 2.35);
        AssertPosAngle(chain, (-128, 192, 0), 2.35);
        AssertPosAngle(shot, (224, 384, 0), 2.35);
        AssertPosAngle(knight, (7.764, 373.01, 0), 2.35);
        AssertPosAngle(Player, pos, Player.AngleRadians);
    }

    [Fact(DisplayName = "Teleport group move activator")]
    public void TeleportGroupMoveActivator()
    {
        var pos = Player.Position;
        var eye = GameActions.GetEntity(World, "EvilEye");
        var col = GameActions.GetEntity(World, "Column");
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        var chain = GameActions.GetEntity(World, "ChaingunGuy");
        var shot = GameActions.GetEntity(World, "ShotgunGuy");
        var knight = GameActions.GetEntity(World, "HellKnight");
        GameActions.ActivateLine(World, Player, 32, ActivationContext.UseLine);
        AssertPosAngle(eye, (-176, 368, 0), 2.35);
        AssertPosAngle(col, (-80, 464, 0), 2.35);
        AssertPosAngle(zombie, (16, 464, 0), 2.35);
        AssertPosAngle(imp, (112, 368, 0), 2.35);
        AssertPosAngle(chain, (-128, 192, 0), 2.35);
        AssertPosAngle(shot, (224, 384, 0), 2.35);
        AssertPosAngle(knight, (288, 448, 0), 2.35);
        Player.Position.Should().NotBe(pos);
    }

    private static void AssertPosAngle(Entity entity, Vec3D pos, double angle)
    {
        entity.Position.X.Should().BeApproximately(pos.X, 2);
        entity.Position.Y.Should().BeApproximately(pos.Y, 2);
        entity.AngleRadians.Should().BeApproximately(angle, 2);
    }
}
