using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class InstantSectorMove
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        public InstantSectorMove()
        {
            World = WorldAllocator.LoadMap("Resources/instantmove.zip", "instantmove.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        [Fact(DisplayName = "Toggle instant floor should not interpolate")]
        public void ToggleInstantFloor()
        {
            Sector sector = GameActions.GetSector(World, 1);
            sector.Floor.Z.Should().Be(64);
            sector.Floor.PrevZ.Should().Be(64);

            GameActions.EntityUseLine(World, Player, 50).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(0);
            sector.Floor.PrevZ.Should().Be(0);

            GameActions.EntityUseLine(World, Player, 64).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(64);
            sector.Floor.PrevZ.Should().Be(64);
        }

        [Fact(DisplayName = "Toggle instant ceiling should not interpolate")]
        public void ToggleInstantCeiling()
        {
            Sector sector = GameActions.GetSector(World, 4);
            sector.Ceiling.Z.Should().Be(128);
            sector.Ceiling.PrevZ.Should().Be(128);

            GameActions.EntityUseLine(World, Player, 61).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Ceiling.Z.Should().Be(0);
            sector.Ceiling.PrevZ.Should().Be(0);

            GameActions.EntityUseLine(World, Player, 67).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Ceiling.Z.Should().Be(128);
            sector.Ceiling.PrevZ.Should().Be(128);
        }

        [Fact(DisplayName = "Floor move should interpolate")]
        public void FloorMove()
        {
            Sector sector = GameActions.GetSector(World, 2);
            sector.Floor.Z.Should().Be(64);
            sector.Floor.PrevZ.Should().Be(64);

            GameActions.EntityUseLine(World, Player, 53).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(60);
            sector.Floor.PrevZ.Should().Be(64);
            GameActions.RunFloorLower(World, sector, 0, 32);
            sector.Floor.Z.Should().Be(0);
            sector.Floor.PrevZ.Should().Be(4);

            GameActions.EntityUseLine(World, Player, 65).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(4);
            sector.Floor.PrevZ.Should().Be(0);
            GameActions.RunFloorRaise(World, sector, 64, 32);
            sector.Floor.Z.Should().Be(64);
            sector.Floor.PrevZ.Should().Be(60);
        }

        [Fact(DisplayName = "Floor moves in a single tick should interpolate")]
        public void FloorMoveSingleTick()
        {
            Sector sector = GameActions.GetSector(World, 3);
            sector.Floor.Z.Should().Be(8);
            sector.Floor.PrevZ.Should().Be(8);

            GameActions.EntityUseLine(World, Player, 57).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(0);
            sector.Floor.PrevZ.Should().Be(8);
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(0);
            sector.Floor.PrevZ.Should().Be(0);

            GameActions.EntityUseLine(World, Player, 66).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(8);
            sector.Floor.PrevZ.Should().Be(0);
            GameActions.TickWorld(World, 1);
            sector.Floor.Z.Should().Be(8);
            sector.Floor.PrevZ.Should().Be(8);
        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }
    }
}
