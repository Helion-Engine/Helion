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

        [Fact(DisplayName = "Entities are spawned correctly with init flag set")]
        public void EntityInitSpawn()
        {
            // Doom does not have a z value set while ZDoom does. Test for clamping issues.
            var imp1 = GameActions.GetEntity(World, 1);
            var imp2 = GameActions.GetEntity(World, 2);
            var imp3 = GameActions.GetEntity(World, 3);

            imp1.HighestFloorZ.Should().Be(0);
            imp2.HighestFloorZ.Should().Be(0);
            imp3.HighestFloorZ.Should().Be(0);

            imp1.LowestCeilingZ.Should().Be(128);
            imp2.LowestCeilingZ.Should().Be(128);
            imp3.LowestCeilingZ.Should().Be(128);

            var sector = GameActions.GetSector(World, 9);
            imp1.HighestFloorObject.Should().Be(sector);
            imp2.HighestFloorObject.Should().Be(sector);
            imp3.HighestFloorObject.Should().Be(sector);

            imp1.LowestCeilingObject.Should().Be(sector);
            imp2.LowestCeilingObject.Should().Be(sector);
            imp3.LowestCeilingObject.Should().Be(sector);

            imp1.IsCrushing().Should().Be(false);
            imp2.IsCrushing().Should().Be(false);
            imp3.IsCrushing().Should().Be(false);
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
            GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
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
            sector.Floor.Z.Should().Be(113);
            sector.Ceiling.Z.Should().Be(113);
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

        [Fact(DisplayName = "Floor dest higher than ceiling with blocking entity")]
        public void FloorDestHigherThanCeiling()
        {
            var imp = GameActions.GetEntity(World, 4);
            var liftSector = GameActions.GetSectorByTag(World, 7);
            GameActions.EntityUseLine(World, Player, 52).Should().BeTrue();

            liftSector.ActiveFloorMove.Should().NotBeNull();

            // The floor should not move at all according to vanilla, even though it can move up 8 map units in this case
            GameActions.TickWorld(World, 35);
            liftSector.Floor.Z.Should().Be(0);
            imp.Position.Z.Should().Be(0);
            liftSector.ActiveFloorMove.Should().BeNull();

            imp.Kill(null);

            GameActions.GetLine(World, 52).SetActivated(false);
            GameActions.EntityUseLine(World, Player, 52).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            liftSector.Floor.Z.Should().Be(64);
            imp.Position.Z.Should().Be(64);
            liftSector.ActiveFloorMove.Should().BeNull();
        }
    }
}
