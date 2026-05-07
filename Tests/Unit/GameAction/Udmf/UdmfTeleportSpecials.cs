using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
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
    private readonly NoRandom Random = new();
    private Player Player => World.Player;

    public UdmfTeleportSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmfteleportspecials.zip", "udmfteleportspecials.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
        World.SetRandom(Random);
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

    [Fact(DisplayName = "Teleport other random spots")]
    public void TeleportOtherRandomSpots()
    {
        Vec3D[] positions = [new Vec3D(1280, 320, 8), new Vec3D(1088, 320, 8), new Vec3D(896, 320, 8)];
        double[] angles = [4.71, 1.57, 3.14];
        var demon = GameActions.GetEntityByTid(World, 101);
        GameActions.SetEntityPosition(World, demon, (896, 128));

        for (int i = 0; i < 6; i++)
        {
            Random.RandomValue = i;

            // Entity can randomly be teleported to the same spot
            for (int j = 0; j < 2; j++)
            {
                GameActions.ActivateLine(World, Player, 55, ActivationContext.UseLine).Should().BeTrue();
                AssertPosAngle(demon, positions[i % positions.Length], angles[i % angles.Length]);
                World.Tick();
            }
        }
    }

    [Fact(DisplayName = "Teleport in sector angles")]
    public void TeleportInSectorAngles()
    {
        Vec3D[] positions = [new Vec3D(1855.52, -51.41, 8), new Vec3D(1719.74, 84.35, 8), new Vec3D(1584, 220.11, 8)];
        double[] angles = [3.92, 0.78, 2.35];
        var demon = GameActions.GetEntityByTid(World, 101);
        GameActions.SetEntityPosition(World, demon, (896, 128));

        for (int i = 0; i < 3; i++)
        {
            Random.RandomValue = i;
            GameActions.ActivateLine(World, Player, 55, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            GameActions.ActivateLine(World, Player, 71, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            AssertPosAngle(demon, positions[i % positions.Length], angles[i % angles.Length]);
            GameActions.ActivateLine(World, Player, 71, ActivationContext.UseLine).Should().BeFalse();
        }
    }

    private static void AssertPosAngle(Entity entity, Vec3D pos, double angle)
    {
        entity.Position.X.Should().BeApproximately(pos.X, 2);
        entity.Position.Y.Should().BeApproximately(pos.Y, 2);
        entity.AngleRadians.Should().BeApproximately(angle, 2);
    }
}
