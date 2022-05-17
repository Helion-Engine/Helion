using FluentAssertions;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        [Fact(DisplayName = "Door opens with ceiling starting below floor")]
        public void DoorCeilingBelowFloor()
        {
            var sector = GameActions.GetSectorByTag(World, 13);
            sector.Ceiling.Z.Should().Be(-8);
            GameActions.ActivateLine(World, Player, 301, ActivationContext.UseLine).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, 8, true);
            sector.Ceiling.Z.Should().Be(124);
        }

        [Fact(DisplayName = "Door open with low adjecent ceiling")]
        public void DoorOpensDown()
        {
            // Vanilla quirk. If the ceiling is at 0 and the lowest ceiling is 0 the door lip will cause the dest to go down. 
            // Normally with VanillaSectorPhysics = false planes can't clip through.
            // Testing with other ports there is apparently an exception for doors (except when the floor is also moving).
            var sector = GameActions.GetSectorByTag(World, 14);
            sector.Ceiling.Z.Should().Be(0);
            GameActions.ActivateLine(World, Player, 307, ActivationContext.UseLine).Should().BeTrue();
            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Ceiling.Z.Should().Be(-4);
        }

        [Fact(DisplayName = "Door open/close with low adjecent ceiling")]
        public void DoorOpenCloseDown()
        {
            var sector = GameActions.GetSectorByTag(World, 15);
            sector.Ceiling.Z.Should().Be(0);
            GameActions.ActivateLine(World, Player, 319, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            // Instantly hits lowest ceiling (past dest)
            sector.Ceiling.Z.Should().Be(-132);
            // 1 tick delay
            World.Tick();
            World.Tick();
            // Instantly returns (past dest)
            sector.Ceiling.Z.Should().Be(0);
        }
    }
}
