using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class TickOrder
{
    private readonly SinglePlayerWorld World;

    public TickOrder()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Entity moves before frame state")]
    public void EntityMove()
    {
        var cleared = false;
        void ClearVelocity(Entity entity)
        {
            entity.Velocity = Vec3D.Zero;
            cleared = true;
        }

        var frames = World.ArchiveCollection.EntityFrameTable.Frames.Where(x => x.ActionFunction == EntityActionFunctions.A_Look).ToArray();
        foreach (var frame in frames)
        {
            frame.Ticks = 1;
            frame.ActionFunction = ClearVelocity;
        }

        // A_Look instead calls ClearVelocity.
        // Move should be called before frame state ticks so the position
        var entity = GameActions.CreateEntity(World, "ZombieMan", new(-320, -384, 0));
        entity.Velocity.Y = 1;
        World.Tick();
        cleared.Should().BeTrue();
        entity.Velocity.Should().Be(Vec3D.Zero);
        entity.Position.Y.Should().Be(-383);
    }


    [Fact(DisplayName = "Player moves in same tick when receiving move command")]
    public void PlayerMove()
    {
        GameActions.SetEntityPosition(World, World.Player, (-320, -384));
        World.Player.Velocity.Should().Be(Vec3D.Zero);

        World.Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        World.Player.TickCommand.Add(TickCommands.Forward);
        World.SetTickCommand(World.Player, World.Player.TickCommand);
        World.Tick();
        World.Player.Position.Y.Should().NotBe(-384);
    }
}
