using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Zdoom;

public class BrightmapDefinition
{
    public string TargetTexture { get; set; } = "";
    public string BrightmapName { get; set; } = "";
    // TODO: needed?
    public string BrightmapFilename { get; set; } = "";
    public bool IwadOnly { get; set; }
    public string? SpecificWad { get; set; }
    public bool DisableFullbright { get; set; }
}

public class BrightmapDefinitions
{
    public Dictionary<string, BrightmapDefinition> Flats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, BrightmapDefinition> Sprites { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, BrightmapDefinition> Textures { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// ZDOOM lump for brightmaps (and possibly dynamic lights and skyboxes in the future)
/// </summary>
/// <seealso href="https://zdoom.org/wiki/GLDEFS"/> 
public class GldefsDefinition
{
    // TODO: handle brightmaps from "auto" folder
    public BrightmapDefinitions BrightMaps = new();

    public void Parse(Entry entry)
    {
        // for GZDoom brightmaps, only parse DOOM's and not Hexen etc
        if (entry.Path.FullPath.StartsWithIgnoreCase("filter/") && !entry.Path.FullPath.StartsWithIgnoreCase("filter/doom.id"))
            return;

        string data = entry.ReadDataAsString();
        SimpleParser parser = new();
        parser.SetSpecialChars(['{', '}']); // remove most special chars, particularly [ ] since they may be part of a sprite name
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string defType = parser.ConsumeString();
            if (defType.EqualsIgnoreCase("#include"))
                ParseInclude(entry, parser);
            else if (defType.EqualsIgnoreCase("brightmap"))
                ParseBrightmapBlock(entry, parser);
            else if (!parser.IsDone())
                parser.ConsumeLine();
        }
    }

    private void ParseInclude(Entry entry, SimpleParser parser)
    {
        string path = parser.ConsumeString();
        Entry? includeEntry = entry.Parent.Entries.FirstOrDefault(x => x.Path.FullPath.EqualsIgnoreCase(path));
        if (includeEntry != null)
            Parse(includeEntry);
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
                // TODO: check
                def.BrightmapName = Path.GetFileNameWithoutExtension(filename);
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
                if (def.BrightmapName != "")
                {
                    var destination = textureType switch
                    {
                        "flat" => BrightMaps.Flats,
                        "sprite" => BrightMaps.Sprites,
                        "texture" => BrightMaps.Textures,
                        _ => null
                    };
                    if (destination != null)
                        destination[def.TargetTexture] = def;
                }
                return;
            }
        }
    }
}
