using FluentAssertions;
using Helion.Resources;
using Helion.Resources.Definitions.Retro;
using Helion.Resources.IWad;
using Xunit;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Tests.Unit.Definitions;

public class RetroBrightmap
{
    string Text1 = @"
BRIGHTMAP NOTGRAY 4,9-79,112-255
BRIGHTMAP NOTGRAYORBROWN 4,9-63,112-125,152-255
BRIGHTMAP NOTGRAYORBROWN2 4,9-63,112-125,152-157,160-255
BRIGHTMAP BLUEGREENBROWNRED 43,65-68,116,121-125,164-167,172,175,205-207,240-245
BRIGHTMAP BLUEGREENBROWN 45,65-68,70,73,76,121-124,164-167,190,206-207,240-241,243
BRIGHTMAP BLUEANDORANGE 45,164-167,190,206-207,240-241,243
BRIGHTMAP REDONLY 40,45-47,173-191
BRIGHTMAP REDONLY2 173-183
BRIGHTMAP GREENONLY1 112-127
BRIGHTMAP GREENONLY2 112-125
BRIGHTMAP GREENONLY3 112-123
BRIGHTMAP YELLOWONLY 160-167,224-231,249
BRIGHTMAP REDANDGREEN 16-47,112-127,173-191
BRIGHTMAP BLUEANDGREEN 112-124,192-207,240-245
BRIGHTMAP BRIGHTTAN 56,58,60-62,64-65,67,69,139,143,147-148,150

TEXTURE COMP2    BLUEANDGREEN
TEXTURE SW2STONE GREENONLY1 DOOM
TEXTURE SW2STONE GREENONLY2 DOOM2
TEXTURE SW2STON2 REDONLY    DOOM1|DOOM2
TEXTURE TEKBRON2 YELLOWONLY DOOM2
TEXTURE SW2SATYR BRIGHTTAN  DOOM1

SPRITE BON2 GREENONLY1
SPRITE CELL GREENONLY2

FLAT CONS1_1 NOTGRAYORBROWN
FLAT CONS1_5 NOTGRAYORBROWN

STATE 84 REDONLY // S_BFG1";

    [Fact(DisplayName = "Parse BRGHTMP")]
    public void ParseRetroBrightmapCommon()
    {
        var def = new RetroBrightmapsDefinition();
        def.Parse(Text1, IWadBaseType.Doom1);
        def.TryGetFullBright("NOTGRAY", out _).Should().BeTrue();
        def.TryGetFullBright("NOTGRAYORBROWN", out _).Should().BeTrue();
        def.TryGetFullBright("NOTGRAYORBROWN2", out _).Should().BeTrue();
        def.TryGetFullBright("BLUEGREENBROWNRED", out _).Should().BeTrue();
        def.TryGetFullBright("BLUEGREENBROWN", out _).Should().BeTrue();
        def.TryGetFullBright("BLUEANDORANGE", out _).Should().BeTrue();
        def.TryGetFullBright("REDONLY", out _).Should().BeTrue();
        def.TryGetFullBright("REDONLY2", out _).Should().BeTrue();
        def.TryGetFullBright("GREENONLY1", out _).Should().BeTrue();
        def.TryGetFullBright("GREENONLY2", out _).Should().BeTrue();
        def.TryGetFullBright("GREENONLY3", out _).Should().BeTrue();
        def.TryGetFullBright("YELLOWONLY", out _).Should().BeTrue();
        def.TryGetFullBright("REDANDGREEN", out _).Should().BeTrue();
        def.TryGetFullBright("BLUEANDGREEN", out _).Should().BeTrue();
        def.TryGetFullBright("BRIGHTTAN", out _).Should().BeTrue();
        def.TryGetFullBright("yomama", out _).Should().BeFalse();

        def.TryGetTextureFullBright(ResourceNamespace.Textures, "COMP2", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Sprites, "BON2", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Sprites, "CELL", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Flats, "CONS1_1", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Flats, "CONS1_5", out _).Should().BeTrue();

        def.TryGetTextureFullBright(ResourceNamespace.Textures, "COMPSPAN", out _).Should().BeFalse();
        def.TryGetTextureFullBright(ResourceNamespace.Sprites, "POSS", out _).Should().BeFalse();
        def.TryGetTextureFullBright(ResourceNamespace.Flats, "FLOOR3_3", out _).Should().BeFalse();

        def.TryGetBrightmapStateName((int)ThingState.BFG1, out _).Should().BeTrue();
        def.TryGetBrightmapStateName((int)ThingState.BFG2, out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Parse BRGHTMP DOOM1")]
    public void ParseRetroBrightmapDoom1()
    {
        var def = new RetroBrightmapsDefinition();
        def.Parse(Text1, IWadBaseType.Doom1);

        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STONE", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STON2", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STONE", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2SATYR", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "TEKBRON2", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Parse BRGHTMP DOOM2")]
    public void ParseRetroBrightmapDoom2()
    {
        var def = new RetroBrightmapsDefinition();
        def.Parse(Text1, IWadBaseType.Doom2);

        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STONE", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STON2", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2STONE", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "TEKBRON2", out _).Should().BeTrue();
        def.TryGetTextureFullBright(ResourceNamespace.Textures, "SW2SATYR", out _).Should().BeFalse();
    }
}
