using System.IO;
using System.Linq;
using FluentAssertions;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Zdoom;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Definitions;

public class GldefsTests
{
    private const string Resource = "Resources/gldefs.zip";

    [Fact(DisplayName = "GLDEFS parsing")]
    public void ParseGldefs()
    {
        GldefsDefinition definition = new();
        IndexGenerator m_indexGenerator = new();

        var archive = new PK3(new EntryPath(Resource), m_indexGenerator);
        var gldefsLump = archive.Entries.Find(x => x.Path.Name.EqualsIgnoreCase("gldefs"));
        gldefsLump.Should().NotBeNull();

        definition.Parse(gldefsLump!);

        definition.BrightMaps.Flats.Count.Should().Be(1);
        definition.BrightMaps.Sprites.Count.Should().Be(1);
        definition.BrightMaps.Textures.Count.Should().Be(1);

        var sprite = definition.BrightMaps.Sprites.First().Value;
        sprite.TargetTexture.Should().Be("POSSA1");
        sprite.BrightmapName.Should().Be("POSSA1");
        sprite.BrightmapFilename.Should().Be("brightmaps/enemies/zombieman/POSSA1.png");
        sprite.IwadOnly.Should().Be(true);

        var texture = definition.BrightMaps.Textures.First().Value;
        texture.TargetTexture.Should().Be("BRICKLIT");
        texture.BrightmapName.Should().Be("BRICKLIT");
        texture.BrightmapFilename.Should().Be("brightmaps/level/BRICKLIT.png");
        texture.SpecificWad.Should().Be(Path.GetFileNameWithoutExtension(Resource));

        var flat = definition.BrightMaps.Flats.First().Value;
        flat.TargetTexture.Should().Be("GATE2");
        flat.BrightmapName.Should().Be("GATE2");
        flat.BrightmapFilename.Should().Be("brightmaps/level/GATE2.png");
    }
}