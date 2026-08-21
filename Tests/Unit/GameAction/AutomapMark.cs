using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class AutomapMark
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public AutomapMark()
    {
        World = WorldAllocator.LoadMap("Resources/bsptest.zip", "bsptest.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.Config.Window.Virtual.Enable.Set(false);
        world.Config.Window.Dimension.Set((640, 480));
    }

    [Fact(DisplayName = "Automap mark room lines")]
    public void TestMarkRoom()
    {
        var marker = CreateAutomapMarker();
        GameActions.SetEntityPosition(World, Player, (340, -96, 384));
        Player.AngleRadians = GameActions.GetAngle(Bearing.SouthEast);
        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [90, 94, 128, 129, 130, 131, 132, 133, 135, 140, 141, 142, 143, 147, 148, 149]);

        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [90, 94, 95, 127, 128, 129, 130, 131, 132, 133, 135, 140, 141, 142, 143, 147, 148, 149]);
    }

    [Fact(DisplayName = "Automap mark room lines open lift")]
    public void TestMarkRoomOpenLift()
    {
        var marker = CreateAutomapMarker();
        GameActions.SetEntityPosition(World, Player, (340, -96, 384));
        Player.AngleRadians = GameActions.GetAngle(Bearing.SouthEast);

        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(384);
        sector.Floor.Z = 0;

        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [90, 94, 128, 129, 130, 131, 132, 133, 134, 135, 136, 138, 139, 140, 141, 142, 143, 144, 147, 148, 149, 172, 176, 177, 178, 179]);
    }

    [Fact(DisplayName = "Automap mark room lines open door")]
    public void TestMarkRoomOpenDoor()
    {
        var marker = CreateAutomapMarker();
        GameActions.SetEntityPosition(World, Player, (380, -132, 384));
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthEast);

        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [90, 93, 94, 127, 128, 129, 130, 132]);

        var sector = GameActions.GetSectorByTag(World, 2);
        sector.Ceiling.Z.Should().Be(384);
        sector.Ceiling.Z = 456;

        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [2, 3, 4, 5, 6, 7, 8, 9, 11, 14, 15, 16, 17, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 43, 44, 45, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 68, 74, 75, 85, 87, 88, 89, 90, 91, 92, 93, 94, 127, 128, 129, 130, 132]);
    }

    [Fact(DisplayName = "Automap mark room lines with render hack")]
    public void TestMarkRenderHack()
    {
        var marker = CreateAutomapMarker();
        GameActions.SetEntityPosition(World, Player, (696, 212, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthEast);

        marker.AddPosition(Player.Position, World.Player.GetCamera(0).Direction.Double, Player.AngleRadians, Player.PitchRadians, 1);
        AssertSeenLines(marker, [109, 110, 181, 182, 183, 184, 185]);
    }

    private void AssertSeenLines(AutomapMarker marker, int[] lineIds)
    {
        var processed = false;
        marker.PositionProcessed += Marker_PositionProcessed;

        var sw = Stopwatch.StartNew();
        while (!processed)
        {
            if (sw.Elapsed.Seconds > 1)
                throw new Exception("Automap marker took too long");
        }

        var lookup = lineIds.ToHashSet();
        foreach (var line in World.Lines)
            line.SeenForAutomap.Should().Be(lookup.Contains(line.Id));

        void Marker_PositionProcessed(object? sender, PlayerPosition e)
        {
            processed = true;
        }
    }

    private AutomapMarker CreateAutomapMarker()
    {
        var marker = new AutomapMarker();
        marker.Start(World);
        return marker;
    }
}
