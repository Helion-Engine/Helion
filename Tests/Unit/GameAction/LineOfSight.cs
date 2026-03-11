using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class LineOfSight
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private Entity SightThing => GameActions.GetEntity(World, 1);
    private Entity SightThing2 => GameActions.GetEntity(World, 2);
    private Entity SightThing3 => GameActions.GetEntity(World, 3);
    private Entity SightThing4 => GameActions.GetEntity(World, 4);

    public LineOfSight()
    {
        World = WorldAllocator.LoadMap("Resources/los.zip", "los.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
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
        World.GetLineOfSightPlayer(SightThing, allAround: true).Should().Be(Player);
        World.GetLineOfSightPlayer(SightThing, allAround: false).Should().BeNull();

        SightThing.AngleRadians = GameActions.GetAngle(Bearing.East);
        World.GetLineOfSightPlayer(SightThing, allAround: false).Should().BeNull();

        GameActions.SetEntityPosition(World, Player, new Vec2D(-254, -480));
        World.GetLineOfSightPlayer(SightThing, allAround: false).Should().Be(Player);

        GameActions.SetEntityPosition(World, Player, new Vec2D(-257, -480));
        SightThing.AngleRadians = GameActions.GetAngle(Bearing.West);
        World.GetLineOfSightPlayer(SightThing, allAround: false).Should().Be(Player);
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
        GameActions.GetSectorByTag(World, 3).Ceiling.SetZ(0);
        GameActions.SetEntityPosition(World, SightThing, new Vec2D(-96, -128));
        GameActions.SetEntityPosition(World, Player, new Vec2D(-96, -320));
        SightThing.AngleRadians = GameActions.GetAngle(Bearing.South);
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);

        World.GetLineOfSightPlayer(SightThing, false).Should().BeNull();
    }

    [Fact(DisplayName = "Line of sight not obstructed by door")]
    public void LineOfSightDoorNotObstructed()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
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
        GameActions.GetSectorByTag(World, 3).Ceiling.SetZ(0);
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
        var sector = GameActions.GetSectorByTag(World, 3);
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


    [Fact(DisplayName = "Line of sight not blocked by self-referencing sector")]
    public void LineOfSightSelfReferencing()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(1024, -224));
        World.GetLineOfSightPlayer(SightThing2, false).Should().Be(Player);
    }

    [Fact(DisplayName = "Line of sight is blocked when exactly on a vertex")]
    public void LineOfSightExactlyOnVertex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(1664, -224));
        World.GetLineOfSightPlayer(SightThing3, false).Should().BeNull();
        var sector = GameActions.GetSectorByTag(World, 2);
        sector.Floor.Z = 0;
        World.GetLineOfSightPlayer(SightThing3, false).Should().Be(Player);
    }

    [Fact(DisplayName = "Line of sight is not blocked when line floor and ceiling sector z values equal")]
    public void LineOfSightIgnoreLineWithNoBlockmap()
    {
        // This is the setup for the chain breaking sound in Eviternity MAP26
        GameActions.SetEntityPosition(World, Player, new Vec2D(2304, -96));
        SightThing4.Flags.SetNoBlockmap();
        SightThing4.Position.Z.Should().Be(-512);
        World.GetLineOfSightPlayer(SightThing4, false).Should().BeNull();

        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(-512);
        GameActions.ActivateLine(World, Player, 48, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(0);

        SightThing4.Sector.Should().Be(sector);
        SightThing4.Position.Z.Should().Be(-512);
        World.GetLineOfSightPlayer(SightThing4, false).Should().Be(Player);
    }

    [Fact(DisplayName = "Line of sight not obstructed by multiple ledges lower (los long check)")]
    public void LineOfSightComplexLongCheckLower()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(2496, 352));
        var sightThing = GameActions.GetEntity(World, 5);
        World.GetLineOfSightPlayer(sightThing, false).Should().Be(Player);
    }

    [Fact(DisplayName = "Line of sight obstructed by multiple ledges lower (los long check)")]
    public void LineOfSightObstructedComplexLongCheckLower()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(2208, 384));
        Player.Position.Z = -8;
        var sightThing = GameActions.GetEntity(World, 5);
        World.GetLineOfSightPlayer(sightThing, false).Should().BeNull();
    }

    [Fact(DisplayName = "Line of sight not obstructed by multiple ledges higher (los long check)")]
    public void LineOfSightComplexLongCheckHigher()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(2472, 1408));
        var sightThing = GameActions.GetEntity(World, 7);
        World.GetLineOfSightPlayer(sightThing, false).Should().Be(Player);
    }
}
