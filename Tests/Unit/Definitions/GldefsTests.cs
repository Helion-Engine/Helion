using System;
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
    private const string MainResource = "Resources/gldefs.zip";
    private const string LoopResource = "Resources/gldefs-recursion-loop.zip";
    private const string AutoResource = "Resources/gldefs-auto-brightmaps.zip";

    [Fact(DisplayName = "GLDEFS should parse")]
    public void ParseGldefs()
    {
        GldefsDefinition definition = new();
        IndexGenerator m_indexGenerator = new();

        var archive = new PK3(new EntryPath(MainResource), m_indexGenerator);
        var gldefsLump = archive.Entries.Find(x => x.Path.Name.EqualsIgnoreCase("gldefs"));
        gldefsLump.Should().NotBeNull();

        definition.Parse(gldefsLump!, Resources.IWad.IWadBaseType.Doom2);

        definition.BrightMaps.Flats.Count.Should().Be(1);
        definition.BrightMaps.Sprites.Count.Should().Be(1);
        definition.BrightMaps.Textures.Count.Should().Be(1);

        var sprite = definition.BrightMaps.Sprites.First();
        sprite.TargetTexture.Should().Be("POSSA1");
        sprite.BrightmapName.Should().Be("brightmaps/enemies/zombieman/POSSA1.png");
        sprite.IsFullPath.Should().BeTrue();
        sprite.IwadOnly.Should().Be(true);

        var texture = definition.BrightMaps.Textures.First();
        texture.TargetTexture.Should().Be("BRICKLIT");
        texture.BrightmapName.Should().Be("brightmaps/level/BRICKLIT.png");
        texture.IsFullPath.Should().BeTrue();
        texture.SpecificWadMd5.Should().NotBeNull();

        var flat = definition.BrightMaps.Flats.First();
        flat.TargetTexture.Should().Be("GATE2");
        flat.BrightmapName.Should().Be("brightmaps/level/GATE2.png");
        flat.IsFullPath.Should().BeTrue();
    }

    [Fact(DisplayName = "GLDEFS parsing should error for infinitely looping #includes")]
    public void ParseGldefsWithInfiniteLoop()
    {
        GldefsDefinition definition = new();
        IndexGenerator m_indexGenerator = new();

        var archive = new PK3(new EntryPath(LoopResource), m_indexGenerator);
        var gldefsLump = archive.Entries.Find(x => x.Path.Name.EqualsIgnoreCase("gldefs"));
        gldefsLump.Should().NotBeNull();

        try
        {
            definition.Parse(gldefsLump!, Resources.IWad.IWadBaseType.Doom2);
        }
        catch (Exception e) when (e.Message.Contains("infinite loop")) { }
    }

    [Fact(DisplayName = "GLDEFS should support automatic brightmaps")]
    public void LoadAutomaticBrightmaps()
    {
        GldefsDefinition definition = new();
        IndexGenerator m_indexGenerator = new();

        var archive = new PK3(new EntryPath(AutoResource), m_indexGenerator);
        var gldefsLump = archive.Entries.Find(x => x.Path.Name.EqualsIgnoreCase("gldefs"));
        gldefsLump.Should().BeNull();

        definition.AddAutoBrightmaps(archive);
        definition.BrightMaps.Auto.Count.Should().Be(1);
        var brightmap = definition.BrightMaps.Auto.First();
        string name = "blank";
        brightmap.Key.Should().Be(name);
        brightmap.Value.BrightmapName.Should().Be("blank.png");
        brightmap.Value.TargetTexture.Should().Be(name);
        brightmap.Value.IsFullPath.Should().BeFalse();
    }
}
