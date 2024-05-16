using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class LineOfSight
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;
        private Entity SightThing => GameActions.GetEntity(World, 1);

        public LineOfSight()
        {
            World = WorldAllocator.LoadMap("Resources/los.zip", "los.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            GameActions.GetEntity(world, 1).Height = 56;
        }

        [Fact(DisplayName = "Basic line of sight checks")]
        public void BasicLineOfSightChecks()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-256, -256));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-288, -480));
            World.CheckLineOfSight(SightThing, Player).Should().BeTrue();

            SightThing.AngleRadians = GameActions.GetAngle(Bearing.North);
            World.GetLineOfSightPlayer(SightThing, allaround: true).Should().Be(Player);
            World.GetLineOfSightPlayer(SightThing, allaround: false).Should().BeNull();

            SightThing.AngleRadians = GameActions.GetAngle(Bearing.East);
            World.GetLineOfSightPlayer(SightThing, allaround: false).Should().BeNull();

            GameActions.SetEntityPosition(World, Player, new Vec2D(-254, -480));
            World.GetLineOfSightPlayer(SightThing, allaround: false).Should().Be(Player);

            GameActions.SetEntityPosition(World, Player, new Vec2D(-257, -480));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.West);
            World.GetLineOfSightPlayer(SightThing, allaround: false).Should().Be(Player);
        }

        [Fact(DisplayName = "Line of sight obstructed by one sided line")]
        public void LineOfSightObstructed()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-416, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-416, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();

            GameActions.SetEntityPosition(World, Player, new Vec2D(-352, -320));
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
        }

        [Fact(DisplayName = "Line of sight not obstructed by one sided line")]
        public void LineOfSightNotObstructed()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-416, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-319, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
        }

        [Fact(DisplayName = "Line of sight obstructed by door")]
        public void LineOfSightDoorObstructed()
        {
            GameActions.GetSector(World, 1).Ceiling.SetZ(0);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-96, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-96, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
        }

        [Fact(DisplayName = "Line of sight not obstructed by door")]
        public void LineOfSightDoorNotObstructed()
        {
            var sector = GameActions.GetSector(World, 1);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-96, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-96, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            for (int i = 0; i < 29; i++)
            {
                sector.Ceiling.SetZ(i);
                World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            }

            sector.Ceiling.SetZ(29);
            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
        }

        [Fact(DisplayName = "Line of sight obstructed by ledge")]
        public void LineOfSightLedgeObstructed()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
        }

        [Fact(DisplayName = "Line of sight partially obstructed by ledge")]
        public void LineOfSightLedge()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();

            for (int i = 0; i < 119; i++)
            {
                GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320 - i));
                World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            }

            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320 - 119));
            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
        }

        [Fact(DisplayName = "Out of sight but in melee distance")]
        public void InMeleeDistance()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -112));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.North);
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();

            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -96));
            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
        }

        [Fact(DisplayName = "Not in custom field of view (90 degrees)")]
        public void NotInFieldOfView()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(464, -64));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.InFieldOfView(SightThing, Player, MathHelper.HalfPi).Should().BeFalse();

            GameActions.SetEntityPosition(World, Player, new Vec2D(304, -64));
            World.InFieldOfView(SightThing, Player, MathHelper.HalfPi).Should().BeFalse();
        }

        [Fact(DisplayName = "In custom field of view (90 degrees)")]
        public void InFieldOfView()
        {
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(464, -120));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.InFieldOfView(SightThing, Player, MathHelper.HalfPi).Should().BeTrue();

            GameActions.SetEntityPosition(World, Player, new Vec2D(304, -120));
            World.InFieldOfView(SightThing, Player, MathHelper.HalfPi).Should().BeTrue();
        }

        const int LineOfSightDistanceTest = 128;


        [Fact(DisplayName = "Line of sight obstructed by door (los short check)")]
        public void LineOfSightDoorObstructed_ShortCheck()
        {
            World.SetLineOfSightDistance(LineOfSightDistanceTest);
            GameActions.GetSector(World, 1).Ceiling.SetZ(0);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-96, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-96, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            World.SetLineOfSightDistance(WorldBase.DefaultLineOfSightDistance);
        }

        [Fact(DisplayName = "Line of sight obstructed by door (los short check)")]
        public void LineOfSightDoorNotObstructed_ShortCheck()
        {
            World.SetLineOfSightDistance(LineOfSightDistanceTest);
            var sector = GameActions.GetSector(World, 1);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(-96, -128));
            GameActions.SetEntityPosition(World, Player, new Vec2D(-96, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);

            for (int i = 0; i < 29; i++)
            {
                sector.Ceiling.SetZ(i);
                World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            }

            sector.Ceiling.SetZ(29);
            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
            World.SetLineOfSightDistance(WorldBase.DefaultLineOfSightDistance);
        }

        [Fact(DisplayName = "Line of sight obstructed by ledge (los short check)")]
        public void LineOfSightLedgeObstructedShortCheck()
        {
            World.SetLineOfSightDistance(LineOfSightDistanceTest);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            World.SetLineOfSightDistance(WorldBase.DefaultLineOfSightDistance);
        }

        [Fact(DisplayName = "Line of sight partially obstructed by ledge (los short check)")]
        public void LineOfSightLedgeShortCheck()
        {
            World.SetLineOfSightDistance(LineOfSightDistanceTest);
            GameActions.SetEntityPosition(World, SightThing, new Vec2D(384, -32));
            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320));
            SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();

            for (int i = 0; i < 119; i++)
            {
                GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320 - i));
                World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
            }

            GameActions.SetEntityPosition(World, Player, new Vec2D(384, -320 - 119));
            World.GetLineOfSightPlayer(SightThing, false).Should().Be(Player);
            World.SetLineOfSightDistance(WorldBase.DefaultLineOfSightDistance);
        }
    }
}
