using FluentAssertions;
using Helion.Demo;
using Helion.Models;
using Helion.Util.SerializationContexts;
using System.Text.Json;
using Xunit;


namespace Helion.Tests.Unit.Util.Demo
{
    public class DemoSerializerTest
    {
        [Fact(DisplayName = "Demo object can be serialized")]
        public void DemoSerializationBasic()
        {
            DemoModel demo = new()
            {
                AppVersion = "three",
                Cheats = [new DemoCheat() { CheatType = 4 }, new DemoCheat() { CheatType = 1 }],
                ConfigValues = [new ConfigValueModel() { Key = "a key", Value = "some value" }],
                GameFiles = new()
                {
                    Files = [new FileModel() { FileName = "eviternity.wad", MD5 = "7" }],
                    IWad = new FileModel() { FileName = "doom.wad", MD5 = "6" },
                },
                Maps = [new DemoMap() { CommandIndex = 1, Map = "A Map", PlayerModel = new() { }, RandomIndex = 10 }],
                Version = DemoVersion.v0960
            };

            string serialized = JsonSerializer.Serialize(demo, typeof(DemoModel), DemoModelSerializationContext.Default);
            serialized.Should().NotBeNullOrEmpty();

            DemoModel? demo2 = JsonSerializer.Deserialize(serialized, typeof(DemoModel), DemoModelSerializationContext.Default) as DemoModel;
            demo2.Should().NotBeNull();

            demo2.AppVersion.Should().BeEquivalentTo(demo.AppVersion);
            demo2.Cheats.Count.Should().Be(demo.Cheats.Count);
            demo2.ConfigValues.Count.Should().Be(demo.ConfigValues.Count);
            demo2.GameFiles.Should().NotBeNull();
            demo2.Maps.Count.Should().Be(demo.Maps.Count);
            demo2.Version.Should().Be(demo.Version);
        }
    }
}
