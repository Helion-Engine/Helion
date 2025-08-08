using FluentAssertions;
using Helion.Resources;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.Archives.Pk3;

public class SpriteChar
{
    [Fact(DisplayName = "Pk3 sprite replace ^")]
    public void SpriteReplace()
    {
        var archive = new PK3(new EntryPath("Resources/spritechar.pk3"), new IndexGenerator());
        archive.Entries.Count.Should().Be(3);
        var globalEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Global);
        var spriteEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Sprites);
        var textureEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Textures);

        globalEntry.Path.FullPath.Should().Be("VILE^1");
        globalEntry.Path.Name.Should().Be("VILE^1");

        spriteEntry.Path.FullPath.Should().Be("Sprites/VILE^1");
        spriteEntry.Path.Name.Should().Be("VILE\\1");

        textureEntry.Path.FullPath.Should().Be("Textures/VILE^1");
        textureEntry.Path.Name.Should().Be("VILE^1");
    }
}
