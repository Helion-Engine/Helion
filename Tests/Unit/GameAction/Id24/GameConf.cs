using FluentAssertions;
using Helion.Resources.Archives.Collection;
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
        World = WorldAllocator.LoadMap("Resources/id24gameconf.zip", "id24gameconf.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, gameConf: true);
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
