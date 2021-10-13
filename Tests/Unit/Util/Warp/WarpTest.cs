using FluentAssertions;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Util;
using Xunit;

namespace Helion.Tests.Unit.Util.Warp;

public class WarpTest
{
    private static readonly string[] EpisodeMaps = { "E1M1", "E1M2", "E2M1", "E2M8", "MAP01", "MAP10", "MAP16", "MAP21" };
    private static readonly string[] Maps = { "MAP01", "MAP10", "MAP16", "MAP21" };

    [Fact(DisplayName = "Episode Warp 11")]
    public void WarpE1M1()
    {
        MapInfo mapInfo = GetEpisodeMapInfo();
        MapWarp.GetMap(11, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");

        MapWarp.GetMap("1 1", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");

        MapWarp.GetMap("1", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");
    }

    [Fact(DisplayName = "Episode Warp 12")]
    public void WarpE1M2()
    {
        MapInfo mapInfo = GetEpisodeMapInfo();
        MapWarp.GetMap(12, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");

        MapWarp.GetMap("1 2", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");

        MapWarp.GetMap("2", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");
    }

    [Fact(DisplayName = "Episode Warp 21")]
    public void WarpE2M1()
    {
        MapInfo mapInfo = GetEpisodeMapInfo();
        MapWarp.GetMap(21, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M1");

        MapWarp.GetMap("2 1", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M1");
    }

    [Fact(DisplayName = "Episode Warp 28")]
    public void WarpE2M8()
    {
        MapInfo mapInfo = GetEpisodeMapInfo();
        MapWarp.GetMap(28, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M8");

        MapWarp.GetMap("2 8", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M8");
    }

    [Fact(DisplayName = "Map Warp 1")]
    public void Warp1()
    {
        MapInfo mapInfo = GetMapInfo();
        MapWarp.GetMap(1, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP01");

        MapWarp.GetMap("1", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP01");
    }

    [Fact(DisplayName = "Map Warp 10")]
    public void Warp10()
    {
        MapInfo mapInfo = GetMapInfo();
        MapWarp.GetMap(10, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP10");

        MapWarp.GetMap("10", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP10");
    }


    [Fact(DisplayName = "Map Warp 16")]
    public void Warp16()
    {
        MapInfo mapInfo = GetMapInfo();
        MapWarp.GetMap(16, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP16");

        MapWarp.GetMap("16", mapInfo, out mapInfoDef);
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP16");
    }

    [Fact(DisplayName = "Map Warp 21")]
    public void Warp21()
    {
        MapInfo mapInfo = GetMapInfo();
        MapWarp.GetMap(21, mapInfo, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP21");

        MapWarp.GetMap("21", mapInfo, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP21");
    }

    private static MapInfo GetEpisodeMapInfo()
    {
        MapInfo mapInfo = new();
        foreach (string mapName in EpisodeMaps)
            mapInfo.AddMap(new MapInfoDef() { MapName = mapName });

        mapInfo.AddEpisode(new EpisodeDef() { Name = "Episode 1", StartMap = "E1M1" });
        mapInfo.AddEpisode(new EpisodeDef() { Name = "Episode 2", StartMap = "E1M2" });
        return mapInfo;
    }

    private static MapInfo GetMapInfo()
    {
        MapInfo mapInfo = new();
        foreach (string mapName in Maps)
            mapInfo.AddMap(new MapInfoDef() { MapName = mapName });

        mapInfo.AddEpisode(new EpisodeDef() { Name = "MAPxx", StartMap = "MAP01" });
        return mapInfo;
    }
}
