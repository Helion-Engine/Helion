using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsScripts
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public AcsScripts()
    {
        World = WorldAllocator.LoadMap("Resources/acs-scripts.zip", "acs-scripts.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.MapInfo.SecretNext = "MAP03";
    }

    [Fact(DisplayName = "PlayerNumber")]
    public void PlayerNumber()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 2 Player 0");
        });
    }

    [Fact(DisplayName = "PrintName")]
    public void PrintName()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(5);
            messages[0].Args.Message.Should().Be("Entryway");
            messages[1].Args.Message.Should().Be("MAP01");
            messages[2].Args.Message.Should().Be("Hurt me plenty.");
            messages[3].Args.Message.Should().Be("MAP02");
            messages[4].Args.Message.Should().Be("MAP03");
        });
    }

    [Fact(DisplayName = "ActivatorTID")]
    public void ActivatorTid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 3 0");

            World.EntityManager.SetThingId(Player, 69);
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(2);
            messages[1].Args.Message.Should().Be("Script 3 69");
        });
    }

    [Fact(DisplayName = "Floor lowers with ActivatorTID = 1 only")]
    public void ActivatorTidLowersFloor()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, Player, 14, ActivationContext.CrossLine).Should().BeTrue();
        World.Tick();
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, imp, 14, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.TickWorld(World, 2);
        sector.Floor.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster activates script with print should not print")]
    public void MonsterActivateScriptPrint()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, imp, 8, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(0);
        });
    }

    [Fact(DisplayName = "ThingCount no tid")]
    public void ThingCountNoTid()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        var zombies = GameActions.GetEntities(World, "Zombieman");
        zombies.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 23, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        zombies[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        zombies[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCount tid")]
    public void ThingCountTid()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        var demons = GameActions.GetEntities(World, "Demon").OrderBy(x => x.ThingId).ToList();
        demons.Count.Should().Be(3);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 36, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        demons[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        demons[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        demons[2].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "UniqueTid")]
    public void UniqueTid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 48, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            int.TryParse(messages[0].Args.Message, out var tid).Should().BeTrue();
            World.EntityManager.TidInUse(tid).Should().BeFalse();
        });
    }

    [Fact(DisplayName = "IsTidUsed")]
    public void IsTidUsed()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 52, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(4);
            messages[0].Args.Message.Should().Be("Tid 1 in use");
            messages[1].Args.Message.Should().Be("Tid 2 not in use");
            messages[2].Args.Message.Should().Be("Tid 4 in use");
            messages[3].Args.Message.Should().Be("Tid 42069 not in use");
        });
    }

    [Fact(DisplayName = "ActorXYZ by activator")]
    public void ActorXYZ_Activator()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.SetEntityPosition(World, Player, (2272, -176, 0));
            GameActions.ActivateLine(World, Player, 60, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("2272, -176, 0");
        });
    }

    [Fact(DisplayName = "ActorXYZ by tid")]
    public void ActorXYZ_Tid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 64, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("2368, 64, 0");
        });
    }

    [Fact(DisplayName = "SetActorPosition by activator with fog")]
    public void SetActorPositionActivator()
    {
        Player.Position.Should().NotBe(new Vec3D(2256, 192, 32));
        GameActions.ActivateLine(World, Player, 68, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Position.Should().Be(new Vec3D(2256, 192, 32));
        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(2);
        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.TickWorld(World, 70);
    }

    [Fact(DisplayName = "SetActorPosition by tid no fog")]
    public void SetActorPositionTid()
    {
        var barrel = GameActions.GetEntityByTid(World, 6);
        barrel.Position.Should().NotBe(new Vec3D(2256, 192, 32));
        GameActions.ActivateLine(World, Player, 72, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        barrel.Position.Should().Be(new Vec3D(2256, 192, 32));
        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(0);
        GameActions.SetEntityOutOfBounds(World, barrel);
    }
}
