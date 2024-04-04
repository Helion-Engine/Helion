using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class VanillaMovementPhysics
{
    private static readonly string ResourceZip = "Resources/vanillamovementphysics.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public VanillaMovementPhysics()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "vanillamovementphysics.wad", MapName, GetType().Name,
            (world) => { world.Config.Compatibility.VanillaMovementPhysics.Set(true); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Velocity is not cleared when hitting thing")]
    public void VanillaPhysicsVelocityNotClearedThing()
    {
        Player.Velocity = (0, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
    }

    [Fact(DisplayName = "Velocity is cleared when hitting thing (vanilla movement physics off)")]
    public void VanillaPhysicsVelocityClearedThing()
    {
        World.Config.Compatibility.VanillaMovementPhysics.Set(false);
        Player.Velocity = (0, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().Be(Vec3D.Zero);
    }

    [Fact(DisplayName = "Velocity is not cleared when hitting line while hitting thing")]
    public void VanillaPhysicsVelocityNotClearedThingAndLine()
    {
        Player.Velocity = (30, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
    }

    [Fact(DisplayName = "Velocity is cleared when hitting line while hitting thing  (vanilla movement physics off)")]
    public void VanillaPhysicsVelocityClearedThingAndLine()
    {
        World.Config.Compatibility.VanillaMovementPhysics.Set(false);
        Player.Velocity = (30, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().Be(Vec3D.Zero);
    }

    [Fact(DisplayName = "Velocity is cleared when hitting line")]
    public void VanillaPhysicsVelocityClearedLine()
    {
        GameActions.SetEntityPosition(World, Player, (-320, -24));
        Player.Velocity = (0, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().Be(Vec3D.Zero);
    }
}