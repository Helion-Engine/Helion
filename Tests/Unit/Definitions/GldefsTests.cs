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

        definition.Parse(gldefsLump!, Resources.IWad.IWadBaseType.Doom2);

        definition.BrightMaps.Flats.Count.Should().Be(1);
        definition.BrightMaps.Sprites.Count.Should().Be(1);
        definition.BrightMaps.Textures.Count.Should().Be(1);

        var sprite = definition.BrightMaps.Sprites.First();
        sprite.TargetTexture.Should().Be("POSSA1");
        sprite.BrightmapName.Should().Be("POSSA1");
        sprite.IwadOnly.Should().Be(true);

        var texture = definition.BrightMaps.Textures.First();
        texture.TargetTexture.Should().Be("BRICKLIT");
        texture.BrightmapName.Should().Be("BRICKLIT");
        texture.SpecificWadMd5.Should().NotBeNull();

        var flat = definition.BrightMaps.Flats.First();
        flat.TargetTexture.Should().Be("GATE2");
        flat.BrightmapName.Should().Be("GATE2");
    }
}
