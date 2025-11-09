using FluentAssertions;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.ParseMapInfo;

public class MapInfoTests
{
    [Fact(DisplayName = "Doom 2 MapInfo")]
    public void Doom2()
    {
        var def = GetMapInfo("MapInfo/Doom2.txt");
        def.MapInfo.Maps.Count.Should().Be(32);

        def.MapInfo.Episodes.Count.Should().Be(1);
        def.MapInfo.Episodes[0].StartMap.EqualsIgnoreCase("MAP01").Should().BeTrue();

        AssertOrder(def.MapInfo.GetOrderedMaps(), ["MAP01", "MAP02", "MAP03", "MAP04", "MAP05", "MAP06", "MAP07", "MAP08", "MAP09", "MAP10", "MAP11", "MAP12", 
            "MAP13", "MAP14", "MAP15", "MAP31", "MAP32", "MAP16", "MAP17", "MAP18", "MAP19", "MAP20", "MAP21", "MAP22", "MAP23", "MAP24", "MAP25", "MAP26", 
            "MAP27", "MAP28", "MAP29", "MAP30"]);

        AssertEndGame(def, "MAP01", "MAP30");
        AssertEndGame(def, "MAP16", "MAP30");
        AssertEndGame(def, "MAP30", "MAP30");
        AssertEndGame(def, "MAP31", "MAP30");
    }

    [Fact(DisplayName = "Doom 1 MapInfo")]
    public void Doom1()
    {
        var def = GetMapInfo("MapInfo/Doom1.txt");
        def.MapInfo.Maps.Count.Should().Be(36);

        def.MapInfo.Episodes.Count.Should().Be(4);
        def.MapInfo.Episodes[0].StartMap.EqualsIgnoreCase("E1M1").Should().BeTrue();
        def.MapInfo.Episodes[1].StartMap.EqualsIgnoreCase("E2M1").Should().BeTrue();
        def.MapInfo.Episodes[2].StartMap.EqualsIgnoreCase("E3M1").Should().BeTrue();
        def.MapInfo.Episodes[3].StartMap.EqualsIgnoreCase("E4M1").Should().BeTrue();

        AssertOrder(def.MapInfo.GetOrderedMaps(), 
        [
            "E1M1", "E1M2", "E1M3", "E1M9", "E1M4", "E1M5", "E1M6", "E1M7", "E1M8",
            "E2M1", "E2M2", "E2M3", "E2M4", "E2M5", "E2M9", "E2M6", "E2M7", "E2M8",
            "E3M1", "E3M2", "E3M3", "E3M4", "E3M5", "E3M6", "E3M9", "E3M7", "E3M8",
            "E4M1", "E4M2", "E4M9", "E4M3", "E4M4", "E4M5", "E4M6", "E4M7", "E4M8",
        ]);

        AssertEndGame(def, "E1M1", "E1M8");
        AssertEndGame(def, "E2M1", "E2M8");
        AssertEndGame(def, "E3M1", "E3M8");
        AssertEndGame(def, "E4M1", "E4M8");
        AssertEndGame(def, "E4M9", "E4M8");
    }

    private static void AssertOrder(List<MapInfoDef> maps, string[] mapNames)
    {
        for(int i = 0; i < mapNames.Length; i++)
            maps[i].MapName.EqualsIgnoreCase(mapNames[i]).Should().BeTrue();
    }

    private static void AssertEndGame(MapInfoDefinition def, string map, string endGameMap)
    {
        var result = def.MapInfo.GetMap(map);
        result.MapInfo.Should().NotBeNull();

        var endgame = def.MapInfo.GetEpisodeEndGame(result.MapInfo!);
        endgame.Should().NotBeNull();
        endgame!.MapName.EqualsIgnoreCase(endGameMap).Should().BeTrue();
    }

    private static MapInfoDefinition GetMapInfo(string mapInfoPath)
    {
        var archive = new PK3(new EntryPath("assets.pk3"), new IndexGenerator());
        var def = new MapInfoDefinition();
        var entry = archive.Entries.FirstOrDefault(x => x.Path.FullPath.EqualsIgnoreCase(mapInfoPath));
        entry.Should().NotBeNull();
        def.Parse(new ArchivePathResolver(archive), entry!.ReadDataAsString(), false);
        return def;
    }

    internal class ArchivePathResolver(Archive archive) : IPathResolver
    {
        private readonly Archive m_archive = archive;

        public Entry? FindEntryByPath(string path) =>
            m_archive.Entries.FirstOrDefault(x => x.Path.FullPath.EqualsIgnoreCase(path));
    }
}
