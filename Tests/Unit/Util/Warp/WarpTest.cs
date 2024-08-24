using FluentAssertions;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs.Impl;
using Helion.World.Util;
using System;
using System.IO;
using Xunit;

namespace Helion.Tests.Unit.Util.Warp;

public class WarpTest
{
    private static readonly string[] EpisodeMaps = { "E1M1", "E1M2", "E2M1", "E2M8", "MAP01", "MAP10", "MAP16", "MAP21" };
    private static readonly string[] Maps = { "MAP01", "MAP10", "MAP16", "MAP21" };

    [Fact(DisplayName = "Episode Warp 11")]
    public void WarpE1M1()
    {
        var archiveCollection = GetArchiveCollection(SetEpisodeMapInfo);
        MapWarp.GetMap(11, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");

        MapWarp.GetMap("1 1", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");

        MapWarp.GetMap("1", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");
    }

    [Fact(DisplayName = "Episode Warp 12")]
    public void WarpE1M2()
    {
        var archiveCollection = GetArchiveCollection(SetEpisodeMapInfo);
        MapWarp.GetMap(12, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");

        MapWarp.GetMap("1 2", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");

        MapWarp.GetMap("2", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M2");
    }

    [Fact(DisplayName = "Episode Warp 21")]
    public void WarpE2M1()
    {
        var archiveCollection = GetArchiveCollection(SetEpisodeMapInfo);
        MapWarp.GetMap(21, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M1");

        MapWarp.GetMap("2 1", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M1");
    }

    [Fact(DisplayName = "Episode Warp 28")]
    public void WarpE2M8()
    {
        var archiveCollection = GetArchiveCollection(SetEpisodeMapInfo);
        MapWarp.GetMap(28, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M8");

        MapWarp.GetMap("2 8", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E2M8");
    }

    [Fact(DisplayName = "Map Warp 1")]
    public void Warp1()
    {
        var archiveCollection = GetArchiveCollection(SetMapInfo);
        MapWarp.GetMap(1, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP01");

        MapWarp.GetMap("1", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP01");
    }

    [Fact(DisplayName = "Map Warp 10")]
    public void Warp10()
    {
        var archiveCollection = GetArchiveCollection(SetMapInfo);
        MapWarp.GetMap(10, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP10");

        MapWarp.GetMap("10", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP10");
    }


    [Fact(DisplayName = "Map Warp 16")]
    public void Warp16()
    {
        var archiveCollection = GetArchiveCollection(SetMapInfo);
        MapWarp.GetMap(16, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP16");

        MapWarp.GetMap("16", archiveCollection, out mapInfoDef);
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP16");
    }

    [Fact(DisplayName = "Map Warp 21")]
    public void Warp21()
    {
        var archiveCollection = GetArchiveCollection(SetMapInfo);
        MapWarp.GetMap(21, archiveCollection, out var mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP21");

        MapWarp.GetMap("21", archiveCollection, out mapInfoDef).Should().BeTrue();
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP21");
    }

    [Fact(DisplayName = "Map Warp without map info")]
    public void MapWarpWithoutMapInfo()
    {
        var archiveCollection = GetArchiveCollection((MapInfo i) => { }, Path.Combine("Resources", "maptest.WAD"));
        MapWarp.GetMap(01, archiveCollection, out var mapInfoDef);
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP01");

        MapWarp.GetMap(02, archiveCollection, out mapInfoDef);
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("MAP02");

        MapWarp.GetMap(11, archiveCollection, out mapInfoDef);
        mapInfoDef.Should().NotBeNull();
        mapInfoDef!.MapName.Should().Be("E1M1");

        MapWarp.GetMap(03, archiveCollection, out mapInfoDef);
        mapInfoDef.Should().BeNull();

        MapWarp.GetMap(12, archiveCollection, out mapInfoDef);
        mapInfoDef.Should().BeNull();
    }

    private static ArchiveCollection GetArchiveCollection(Action<MapInfo> setMapInfo, string? wadFile = null)
    {
        var config = new Config();
        ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(config), config, new DataCache());

        if (wadFile != null)
            archiveCollection.Load(new[] { wadFile }, loadDefaultAssets: false);

        setMapInfo(archiveCollection.Definitions.MapInfoDefinition.MapInfo);
        return archiveCollection;
    }

    private static void SetEpisodeMapInfo(MapInfo mapInfo)
    {
        foreach (string mapName in EpisodeMaps)
            mapInfo.AddOrReplaceMap(new MapInfoDef() { MapName = mapName });

        mapInfo.AddEpisode(new EpisodeDef() { Name = "Episode 1", StartMap = "E1M1" });
        mapInfo.AddEpisode(new EpisodeDef() { Name = "Episode 2", StartMap = "E1M2" });
    }

    private static void SetMapInfo(MapInfo mapInfo)
    {
        foreach (string mapName in Maps)
            mapInfo.AddOrReplaceMap(new MapInfoDef() { MapName = mapName });

        mapInfo.AddEpisode(new EpisodeDef() { Name = "MAPxx", StartMap = "MAP01" });
    }
}
