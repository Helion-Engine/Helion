using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class BoomPhysics
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        public BoomPhysics()
        {
            World = WorldAllocator.LoadMap("Resources/boomphysics.zip", "boomphysics.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.Config.Compatibility.VanillaSectorPhysics.Set(false);
        }

        [Fact(DisplayName = "Floor and ceiling move towards each other until blocked")]
        public void FloorAndCeilingMoveBlock()
        {
            var sector = GameActions.GetSectorByTag(World, 1);
            GameActions.ActivateLine(World, Player, 8, ActivationContext.UseLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 9, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(64);
            sector.Ceiling.Z.Should().Be(64);
        }

        [Fact(DisplayName = "Floor and ceiling elevator")]
        public void FloorAndCeilingElevator()
        {
            var sector = GameActions.GetSectorByTag(World, 2);
            GameActions.ActivateLine(World, Player, 11, ActivationContext.CrossLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 18, ActivationContext.CrossLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(320);
            sector.Ceiling.Z.Should().Be(384);
        }

        [Fact(DisplayName = "Lift and ceiling move towards each other until blocked")]
        public void LiftAndCeilingMoveBlock()
        {
            var sector = GameActions.GetSectorByTag(World, 3);
            GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(49);
            sector.Ceiling.Z.Should().Be(49);

            GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
            GameActions.RunLift(World, sector, 49, 0, 32, 35);
        }

        [Fact(DisplayName = "Door and floor move towards each other until blocked")]
        public void DoorAndFloorMoveBlock()
        {
            var sector = GameActions.GetSectorByTag(World, 4);
            GameActions.ActivateLine(World, Player, 21, ActivationContext.UseLine).Should().BeTrue();
            // Floor move will move to where the door ceiling is at the time of activation.
            GameActions.TickWorld(World, 35*3);
            GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(112);
            sector.Ceiling.Z.Should().Be(112);
        }

        [Fact(DisplayName = "Activate two lines with use through flag")]
        public void PlayerUseThrough()
        {
            var liftSector = GameActions.GetSectorByTag(World, 5);
            var crushSector = GameActions.GetSectorByTag(World, 6);
            GameActions.EntityUseLine(World, Player, 44).Should().BeTrue();

            liftSector.ActiveFloorMove.Should().NotBeNull();
            crushSector.ActiveCeilingMove.Should().NotBeNull();
        }
    }
}
