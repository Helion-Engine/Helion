using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.World.Impl.SinglePlayer;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class GameConf
{
    private readonly SinglePlayerWorld World;

    public GameConf()
    {
        World = WorldAllocator.LoadMap("Resources/id24gameconf.zip", "id24gameconf.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, gameConf: true);
    }

    [Fact(DisplayName = "Pwad load order")]
    public void Pwads()
    {
        var archives = World.ArchiveCollection.AllArchives.ToArray();
        archives.Length.Should().Be(5);
        archives[0].Path.Name.ToLower().Should().Be("assets");
        archives[1].Path.Name.ToLower().Should().Be("doom2");
        archives[2].Path.Name.ToLower().Should().Be("id24gameconf");
        archives[3].Path.Name.ToLower().Should().Be("pwad1");
        archives[4].Path.Name.ToLower().Should().Be("pwad2");
    }

    [Fact(DisplayName = "Wad translation")]
    public void WadTranslation()
    {
        World.ArchiveCollection.IWad.Should().NotBeNull();
        World.ArchiveCollection.IWad!.TranslationPalette.Should().NotBeNull();

        AssertArchiveHasTranslationPalette("pwad1.WAD", true);
        AssertArchiveHasTranslationPalette("pwad2.WAD", true);
        AssertArchiveHasTranslationPalette("doom2.wad", true);
        AssertArchiveHasTranslationPalette("id24gameconf.WAD", false);
    }

    private void AssertArchiveHasTranslationPalette(string filename, bool hasPalette)
    {
        var archive = World.ArchiveCollection.AllArchives.FirstOrDefault(x => x.Path.NameWithExtension.EqualsIgnoreCase(filename));
        archive.Should().NotBeNull();
        if (hasPalette)
            archive!.TranslationPalette.Should().NotBeNull();
        else
            archive!.TranslationPalette.Should().BeNull();
    }
}
