using FluentAssertions;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Switch
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;
        private Sector LiftSector => GameActions.GetSectorByTag(World, 1);

        public Switch()
        {
            World = WorldAllocator.LoadMap("Resources/switch.zip", "switch.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }

        [Fact(DisplayName = "Single use switch")]
        public void SingleUseSwitch()
        {
            var line = GameActions.GetLine(World, 9);
            var side = line.Front;
            line.Activated.Should().BeFalse();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1BLUE");
            GameActions.ActivateLine(World, Player, 9, ActivationContext.UseLine).Should().BeTrue();
            line.Activated.Should().BeTrue();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2BLUE");

            LiftSector.ActiveFloorMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, LiftSector);
        }

        [Fact(DisplayName = "Multi use switch")]
        public void MultiUseSwitch()
        {
            const int Line = 8;
            var line = GameActions.GetLine(World, Line);
            var side = line.Front;

            for (int i = 0; i < 2; i++)
            {
                line.Activated.Should().BeFalse();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1BLUE");
                GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
                line.Activated.Should().BeTrue();
                LiftSector.ActiveFloorMove.Should().NotBeNull();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2BLUE");
                GameActions.TickWorld(World, 35, () =>
                {
                    GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeFalse();
                    GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2BLUE");
                    line.Activated.Should().BeFalse();
                });

                World.Tick();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1BLUE");
                GameActions.RunSectorPlaneSpecial(World, LiftSector, () =>
                {
                    GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeFalse();
                });
            }
        }

        [Fact(DisplayName = "Multi use switch activating multiple sectors")]
        public void MultiUseSwitchMultipleSectors()
        {
            const int Line = 24;
            var line = GameActions.GetLine(World, Line);
            var side = line.Front;
            var longSector = GameActions.GetSector(World, 3);
            var shortSector = GameActions.GetSector(World, 4);

            for (int i = 0; i < 2; i++)
            {
                line.Activated.Should().BeFalse();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1COMP");
                GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
                line.Activated.Should().BeTrue();
                longSector.ActiveFloorMove.Should().NotBeNull();
                shortSector.ActiveFloorMove.Should().NotBeNull();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2COMP");

                // When the shorter special completes the line is allowed to activate again, but fails while both sectors are active.
                GameActions.RunSectorPlaneSpecial(World, shortSector, () =>
                {
                    GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeFalse();
                });

                World.Tick();
                GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1COMP");

                shortSector.ActiveFloorMove.Should().BeNull();
                longSector.ActiveFloorMove.Should().NotBeNull();
                // The short sector can be activatetd again, long is still running
                GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
                shortSector.ActiveFloorMove.Should().NotBeNull();

                GameActions.RunSectorPlaneSpecial(World, longSector, () => { });
            }
        }

        [Fact(DisplayName = "Multi use quick switch")]
        public void QuickSwitch()
        {
            // Doom had a quirk where if the switch coudld be activated during the animation it would switch the texture back off
            const int Line = 34;
            var line = GameActions.GetLine(World, Line);
            var side = line.Front;
            var sector = GameActions.GetSectorByTag(World, 3);

            line.Activated.Should().BeFalse();
            sector.ActiveFloorMove.Should().BeNull();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1GARG");
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            line.Activated.Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2GARG");

            World.Tick();
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            line.Activated.Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1GARG");

            World.Tick();
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            line.Activated.Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW2GARG");

            World.Tick();
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            line.Activated.Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1GARG");

            GameActions.RunSectorPlaneSpecial(World, sector, () => { });
            GameActions.TickWorld(World, 35);
            GameActions.CheckSideTexture(World, side, WallLocation.Middle, "SW1GARG");
        }

        [Fact(DisplayName = "Front lower switch texture")]
        public void FrontLowerSwitchTexture()
        {
            const int Line = 39;
            var side = GameActions.GetLine(World, Line).Front;
            var sector = GameActions.GetSectorByTag(World, 4);
            GameActions.CheckSideTexture(World, side, WallLocation.Lower, "SW1LION");
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();

            GameActions.CheckSideTexture(World, side, WallLocation.Lower, "SW2LION");
            GameActions.RunSectorPlaneSpecial(World, sector, () => { });
        }

        [Fact(DisplayName = "Front upper switch texture")]
        public void FrontUpperSwitchTexture()
        {
            const int Line = 43;
            var side = GameActions.GetLine(World, Line).Front;
            var sector = GameActions.GetSectorByTag(World, 4);
            GameActions.CheckSideTexture(World, side, WallLocation.Upper, "SW1ZIM");
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();

            GameActions.CheckSideTexture(World, side, WallLocation.Upper, "SW2ZIM");
            GameActions.RunSectorPlaneSpecial(World, sector, () => { });
        }

        [Fact(DisplayName = "Back lower switch texture")]
        public void BackLowerSwitchTexture()
        {
            const int Line = 53;
            var side = GameActions.GetLine(World, Line).Back!;
            var sector = GameActions.GetSectorByTag(World, 4);
            GameActions.CheckSideTexture(World, side, WallLocation.Lower, "SW1METAL");
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            // Doesn't change
            GameActions.CheckSideTexture(World, side, WallLocation.Lower, "SW1METAL");
            GameActions.RunSectorPlaneSpecial(World, sector, () => { });
        }

        [Fact(DisplayName = "Back upper switch texture")]
        public void BackUpperSwitchTexture()
        {
            const int Line = 56;
            var side = GameActions.GetLine(World, Line).Back!;
            var sector = GameActions.GetSectorByTag(World, 4);
            GameActions.CheckSideTexture(World, side, WallLocation.Upper, "SW1GSTON");
            GameActions.ActivateLine(World, Player, Line, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            // Doesn't change
            GameActions.CheckSideTexture(World, side, WallLocation.Upper, "SW1GSTON");
            GameActions.RunSectorPlaneSpecial(World, sector, () => { });
        }
    }
}
