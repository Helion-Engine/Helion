using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Zdoom;

public class BrightmapDefinition
{
    public string TargetTexture { get; set; } = "";
    public string BrightmapName { get; set; } = "";
    public bool IwadOnly { get; set; }
    /// <summary>Used for `thiswad` option</summary>
    public string? SpecificWadMd5 { get; set; }
    // TODO: handle
    public bool DisableFullbright { get; set; }
}

public class BrightmapDefinitions
{
    public List<BrightmapDefinition> Flats { get; set; } = [];
    public List<BrightmapDefinition> Sprites { get; set; } = [];
    public List<BrightmapDefinition> Textures { get; set; } = [];
}

/// <summary>
/// ZDOOM lump for brightmaps (and possibly dynamic lights and skyboxes in the future)
/// </summary>
/// <seealso href="https://zdoom.org/wiki/GLDEFS"/> 
public class GldefsDefinition
{
    // TODO: handle brightmaps from "auto" folder
    public BrightmapDefinitions BrightMaps = new();

    public void Parse(Entry entry, IWadBaseType iwadType)
    {
        // for GZDoom brightmaps, only parse DOOM's and not Hexen etc
        if (entry.Path.FullPath.StartsWithIgnoreCase("filter/"))
        {
            string[] validFilterPaths = iwadType switch
            {
                IWadBaseType.Doom1 => ["doom.id", "doom.id.doom1"],
                IWadBaseType.Doom2 => ["doom.id", "doom.id.doom2"],
                IWadBaseType.Plutonia => ["doom.id", "doom.id.doom2", "doom.id.doom2.plutonia"],
                IWadBaseType.TNT => ["doom.id", "doom.id.doom2", "doom.id.doom2.tnt"],
                // could be chex.chex1 or chex.chex3 based on https://www.chexquest3.com/downloads/cq3gldef.zip
                IWadBaseType.ChexQuest => ["chex"],
                _ => []
            };
            if (!validFilterPaths.Any(x => entry.Path.FullPath.StartsWithIgnoreCase($"filter/{x}/")))
                return;
        }

        string data = entry.ReadDataAsString();
        SimpleParser parser = new();
        parser.SetSpecialChars(['{', '}']); // remove most special chars, particularly [ ] since they may be part of a sprite name
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string defType = parser.ConsumeString();
            if (defType.EqualsIgnoreCase("#include"))
                ParseInclude(entry, iwadType, parser);
            else if (defType.EqualsIgnoreCase("brightmap"))
                ParseBrightmapBlock(entry, parser);
            else if (!parser.IsDone())
                parser.ConsumeLine();
        }
    }

    private void ParseInclude(Entry entry, IWadBaseType iwadType, SimpleParser parser)
    {
        string path = parser.ConsumeString();
        Entry? includeEntry = entry.Parent.Entries.FirstOrDefault(x => x.Path.FullPath.EqualsIgnoreCase(path));
        if (includeEntry != null)
            Parse(includeEntry, iwadType);
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
            if (token.EqualsIgnoreCase("map"))
            {
                string filename = parser.ConsumeString();
                def.BrightmapName = Path.GetFileNameWithoutExtension(filename);
            }
            else if (token.EqualsIgnoreCase("iwad"))
                def.IwadOnly = true;
            else if (token.EqualsIgnoreCase("thiswad"))
                def.SpecificWadMd5 = entry.Parent.MD5;
            else if (token.EqualsIgnoreCase("disablefullbright"))
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
                    destination?.Add(def);
                }
                return;
            }
        }
    }
}
