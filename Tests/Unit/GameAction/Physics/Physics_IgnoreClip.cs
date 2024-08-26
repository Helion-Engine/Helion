using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public partial class PhysicsIgnoreClip
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        public PhysicsIgnoreClip()
        {
            World = WorldAllocator.LoadMap("Resources/physicsignoreclip.zip", "physicsignoreclip.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        }

        [Fact(DisplayName = "Instant move sector not blocked by clipped entities")]
        public void InstantMoveSectorNotBlockedByClippedEntities()
        {
            var sector = GameActions.GetSectorByTag(World, 2);
            var entities = GameActions.GetSectorEntities(World, 1);

            sector.Floor.Z.Should().Be(-56);
            entities.Count.Should().Be(4);
            foreach (var entity in entities)
                entity.Position.Z.Should().Be(-56);

            GameActions.ActivateLine(World, Player, 8, ActivationContext.CrossLine);
            GameActions.RunSectorPlaneSpecial(World, sector);

            sector.Floor.Z.Should().Be(0);
            foreach (var entity in entities)
                entity.Position.Z.Should().Be(0);
        }
    }
}