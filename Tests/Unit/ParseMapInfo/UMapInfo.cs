using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Xunit;

namespace Helion.Tests.Unit.ParseMapInfo;

public class UMapInfo
{
    private static void SetupMapInfo(MapInfoDefinition mapInfoDef)
    {
        mapInfoDef.MapInfo.AddCluster(new ClusterDef(1)
        {
            ExitText = ["default exit text"],
            SecretExitText = ["default secret exit text"],
        });

        mapInfoDef.MapInfo.AddEpisode(new EpisodeDef()
        {
            Name = "test",
            StartMap = "E3M1"
        });

        mapInfoDef.MapInfo.AddOrReplaceMap(new MapInfoDef()
        {
            MapName = "E1M1",
            NiceName = "Default levelname",
            Label = "Default E1M1 label",
            Next = "Default E1M1 Next",
            Sky1 = new SkyDef() { Name = "Default E1M1 Sky" }
        });

        mapInfoDef.MapInfo.AddOrReplaceMap(new MapInfoDef()
        {
            MapName = "E1M2",
            Cluster = 1
        });

        var bossActions = new List<BossAction>()
        {
            new("BaronOfHell", VanillaLineSpecialType.S1_LowerLiftRaise, 69)
        };

        mapInfoDef.MapInfo.AddOrReplaceMap(new MapInfoDef()
        {
            MapName = "E5M1",
            BossActions = bossActions,
            MapSpecial = MapSpecial.CyberdemonSpecial,
            MapSpecialAction = MapSpecialAction.OpenDoor
        });

        mapInfoDef.MapInfo.AddOrReplaceMap(new MapInfoDef()
        {
            MapName = "E1M8",
            MapSpecial = MapSpecial.BaronSpecial,
            MapSpecialAction = MapSpecialAction.LowerFloor
        });

        mapInfoDef.MapInfo.AddOrReplaceMap(new MapInfoDef()
        {
            MapName = "E2M1",
            TitlePatch = "Default E2M1 titlepatch",
            Label = "Default E2M1 label",
            Next = "Default E2M1 Next",
            Sky1 = new SkyDef() { Name = "Default E1M1 Sky" }
        });

        mapInfoDef.MapInfo.Episodes.Count.Should().Be(1);

        var getMap = mapInfoDef.MapInfo.GetMap("E1M2").MapInfo;
        getMap.Should().NotBeNull();
        var e1m2 = getMap!;
        var cluster = mapInfoDef.MapInfo.GetCluster(e1m2.Cluster);
        cluster.Should().NotBeNull();
        cluster!.ExitText.Count.Should().Be(1);
        cluster!.SecretExitText.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Parse UMapInfo")]
    public void ParseUMapInfo()
    {
        var mapInfoDef = new MapInfoDefinition();
        SetupMapInfo(mapInfoDef);

        mapInfoDef.ParseUniversalMapInfo(IWadBaseType.Doom1, File.ReadAllText("Resources/UMAPINFO1.TXT"));

        var episodes = mapInfoDef.MapInfo.Episodes;
        episodes.Count.Should().Be(2);

        episodes[0].PicName.Should().BeEquivalentTo("M_EPI1");
        episodes[0].StartMap.Should().BeEquivalentTo("E1M1");

        episodes[1].PicName.Should().BeEquivalentTo("M_EPI5");
        episodes[1].StartMap.Should().BeEquivalentTo("E5M1");

        var getMap = mapInfoDef.MapInfo.GetMap("E1M1").MapInfo;
        getMap.Should().NotBeNull();
        var e1m1 = getMap!;
        e1m1.MapName.Should().Be("E1M1");
        e1m1.NiceName.Should().Be("Chemical Circumstances");
        e1m1.Music.Should().Be("D_E1M0");
        e1m1.Label.Should().Be("E1M1 Label");
        e1m1.Next.Should().Be("E1M2");
        e1m1.Sky1.Name.Should().Be("Default E1M1 Sky");
        e1m1.TitlePatch.Should().Be("WILV00");
        e1m1.HasOption(MapOptions.NoIntermission).Should().BeFalse();
        e1m1.Author.Should().Be("the author");
        e1m1.ParTime.Should().Be(420);

        getMap = mapInfoDef.MapInfo.GetMap("E1M2").MapInfo;
        getMap.Should().NotBeNull();
        var e1m2 = getMap!;
        e1m2.MapName.Should().BeEquivalentTo("e1m2");
        e1m2.NiceName.Should().Be("Chemical Storage");
        e1m2.Music.Should().Be("D_E1M2");
        e1m2.Label.Should().Be("");
        e1m2.Next.Should().Be("E1M3");
        e1m2.Sky1.Name.Should().Be("TESTSKY");
        e1m2.TitlePatch.Should().Be("WILV01");
        e1m2.HasOption(MapOptions.NoIntermission).Should().BeTrue();
        e1m2.Author.Should().Be("");
        e1m2.EnterPic.Should().Be("TESTINTERPIC");
        var cluster = e1m2.ClusterDef;
        cluster.Should().BeNull();

        getMap = mapInfoDef.MapInfo.GetMap("E1M3").MapInfo;
        getMap.Should().NotBeNull();
        var e1m3 = getMap!;
        e1m3.ClusterDef.Should().NotBeNull();
        cluster = e1m3.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.SecretExitText[0].Should().Be("super secret");
        cluster!.SecretExitText[1].Should().Be("really secret");

        getMap = mapInfoDef.MapInfo.GetMap("E1M8").MapInfo;
        getMap.Should().NotBeNull();
        var e1m8 = getMap!;
        e1m8.Next.Should().BeEquivalentTo("EndPic");
        e1m8.EndPic.Should().Be("CREDIT");
        e1m8.BossActions.Count.Should().Be(2);
        e1m8.MapSpecial.Should().Be(MapSpecial.None);
        e1m8.MapSpecialAction.Should().Be(MapSpecialAction.None);

        var bossAction = e1m8.BossActions[0];
        bossAction.ActorName.Should().BeEquivalentTo("cyberdemon");
        bossAction.Action.Should().Be(VanillaLineSpecialType.SR_RaiseFloorToNextHigher);
        bossAction.Tag.Should().Be(42069);

        var bossActionEdNum = e1m8.BossActions[1];
        bossActionEdNum.ActorName.Length.Should().Be(0);
        bossActionEdNum.Action.Should().Be(VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage);
        bossActionEdNum.Tag.Should().Be(999);

        cluster = e1m8.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.ExitText[0].Should().Be("Despite your victory, you rot along with the");
        cluster!.ExitText[1].Should().Be("core and everything goes black.");
        cluster!.ExitText[2].Should().Be("");
        cluster!.ExitText[3].Should().Be("You awaken on what appears to be Deimos,");
        cluster!.ExitText[4].Should().Be("but something has changed. Time to get");
        cluster!.ExitText[5].Should().Be("to the bottom of this.");
        cluster!.ExitText[6].Should().Be("");
        cluster!.ExitText[7].Should().Be("Join us for Episode 2: Deimos Corrupted!");

        getMap = mapInfoDef.MapInfo.GetMap("E1M9").MapInfo;
        getMap.Should().NotBeNull();
        var e1m9 = getMap!;
        e1m9.Next.Should().Be("EndBunny");

        getMap = mapInfoDef.MapInfo.GetMap("E5M1").MapInfo;
        getMap.Should().NotBeNull();
        var e5m1 = getMap!;
        // boss actions should be cleared
        e5m1.BossActions.Count.Should().Be(0);
        e5m1.MapSpecial.Should().Be(MapSpecial.None);
        e5m1.MapSpecialAction.Should().Be(MapSpecialAction.None);
        cluster = e5m1.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Music.Should().Be("D_TEST");

        getMap = mapInfoDef.MapInfo.GetMap("E5M8").MapInfo;
        getMap.Should().NotBeNull();
        var e5m8 = getMap!;
        e5m8.Next.Should().BeEquivalentTo("EndGameC");
        e5m8.EndPic.Should().BeEquivalentTo("CREDIT");
        cluster = e5m8.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().BeEquivalentTo("FLAT_69");
        cluster!.ExitText[0].Should().Be("Deimos has become fully corrupted,");
        cluster!.ExitText[1].Should().Be("and you now stand on the threshold");
        cluster!.ExitText[2].Should().Be("of hell!");
        cluster!.ExitText[3].Should().Be("");
        cluster!.ExitText[4].Should().Be("Join us for the next episode: Fever Dream!");

        getMap = mapInfoDef.MapInfo.GetMap("E1M4").MapInfo;
        getMap.Should().NotBeNull();
        var e1m4 = getMap!;
        e1m4.Next.Should().Be("EndGame1");
        cluster = e1m4.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLATE1");
        cluster.ExitText[0].Should().Be("$E1TEXT");

        getMap = mapInfoDef.MapInfo.GetMap("E2M1").MapInfo;
        getMap.Should().NotBeNull();
        var e2m1 = getMap!;
        e2m1.Next.Should().Be("EndGame2");
        e2m1.TitlePatch.Should().Be("Default E2M1 titlepatch");
        e2m1.NiceName.Length.Should().Be(0);
        e2m1.HasOption(MapOptions.NoIntermission).Should().BeFalse();
        cluster = e2m1.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLATE2");
        cluster.ExitText[0].Should().Be("$E2TEXT");

        getMap = mapInfoDef.MapInfo.GetMap("E3M2").MapInfo;
        getMap.Should().NotBeNull();
        var e3m2 = getMap!;
        e3m2.HasOption(MapOptions.NoIntermission).Should().BeTrue();
        e3m2.Next.Should().Be("EndGame3");
        cluster = e3m2.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLATE3");
        cluster.ExitText[0].Should().Be("$E3TEXT");

        getMap = mapInfoDef.MapInfo.GetMap("E3M2").MapInfo;
        getMap.Should().NotBeNull();
        var e3m8 = getMap!;
        e3m8.Next.Should().Be("EndGame3");
        cluster = e3m2.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLATE3");
        cluster.ExitText[0].Should().Be("$E3TEXT");

        getMap = mapInfoDef.MapInfo.GetMap("E4M1").MapInfo;
        getMap.Should().NotBeNull();
        var e4m1 = getMap!;
        e4m1.Next.Should().Be("EndGame4");
        cluster = e4m1.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLATE4");
        cluster.ExitText[0].Should().Be("$E4TEXT");

        getMap = mapInfoDef.MapInfo.GetMap("MAP01").MapInfo;
        getMap.Should().NotBeNull();
        var map01 = getMap!;
        map01.Next.Should().Be("EndGameW");
        cluster = map01.ClusterDef;
        cluster.Should().NotBeNull();
        cluster!.Flat.Should().Be("$BGFLAT30");
        cluster!.ExitText[0].Should().Be("map01 intertext");

        getMap = mapInfoDef.MapInfo.GetMap("MAP02").MapInfo;
        getMap.Should().NotBeNull();
        var map02 = getMap!;
        map02.ClusterDef.Should().BeNull();
    }
}
