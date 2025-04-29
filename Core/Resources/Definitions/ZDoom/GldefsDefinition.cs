using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Zdoom;

public class BrightmapDefinition
{
    public string TargetTexture { get; set; } = "";
    public string BrightmapFilename { get; set; } = "";
    public bool IwadOnly { get; set; }
    public string? SpecificWad { get; set; }
    public bool DisableFullbright { get; set; }
}

public class BrightmapDefinitions
{
    public List<BrightmapDefinition> Sprites { get; set; } = [];
    public List<BrightmapDefinition> Textures { get; set; } = [];
    public List<BrightmapDefinition> Flats { get; set; } = [];
}

/// <summary>
/// ZDOOM lump for brightmaps (and possibly dynamic lights and skyboxes in the future)
/// </summary>
/// <seealso href="https://zdoom.org/wiki/GLDEFS"/> 
public class GldefsDefinition
{
    public BrightmapDefinitions BrightMaps = new();

    public void Parse(Entry entry)
    {
        // for GZDoom brightmaps, only parse DOOM's and not Hexen etc
        if (entry.Path.FullPath.StartsWithIgnoreCase("filter/") && !entry.Path.FullPath.StartsWithIgnoreCase("filter/doom.id/"))
            return;

        string data = entry.ReadDataAsString();
        SimpleParser parser = new();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string defType = parser.ConsumeString();
            if (defType.EqualsIgnoreCase("brightmap"))
                ParseBrightmapBlock(entry, parser);
            else
                parser.ConsumeLine();
        }
    }

    private void ParseBrightmapBlock(Entry entry, SimpleParser parser)
    {
        string textureType = parser.ConsumeString().ToLowerInvariant();
        string texture = parser.ConsumeString();
        parser.ConsumeString("{");
        BrightmapDefinition def = new() { TargetTexture = texture };
        while (!parser.IsDone())
        {
            string token = parser.ConsumeString().ToLowerInvariant();
            if (token == "map")
            {
                string filename = parser.ConsumeString();
                def.BrightmapFilename = filename;
            }
            else if (token == "iwad")
                def.IwadOnly = true;
            else if (token == "thiswad")
                def.SpecificWad = entry.Parent.Path.Name;
            else if (token == "disablefullbright")
                def.DisableFullbright = true;
            else if (token == "}")
            {
                if (def.BrightmapFilename != "")
                {
                    var destination = textureType switch
                    {
                        "flat" => BrightMaps.Flats,
                        "sprite" => BrightMaps.Sprites,
                        "texture" => BrightMaps.Textures,
                        _ => null
                    };
                    destination?.Add(def);
                }
                return;
            }
        }
    }
}
