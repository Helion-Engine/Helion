using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Zdoom;

public class BrightmapDefinition
{
    public string TargetTexture { get; set; } = "";
    // some brightmaps packs will not have a brightmap, but will disable the fullbright
    public string? BrightmapName { get; set; }
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
    /// <summary>Brightmaps placed in brightmaps/auto whose type is indeterminate</summary>
    public Dictionary<string, BrightmapDefinition> Auto { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// ZDOOM lump for brightmaps (and possibly dynamic lights and skyboxes in the future)
/// </summary>
/// <seealso href="https://zdoom.org/wiki/GLDEFS"/> 
public class GldefsDefinition
{
    public BrightmapDefinitions BrightMaps = new();

    private readonly Stack<string> m_includeStack = new();
    private IWadBaseType m_iwadType;

    public void Parse(Entry entry, IWadBaseType iwadType)
    {
        m_iwadType = iwadType;
        // for directory filters (e.g. GZDoom's brightmaps), only parse DOOM's and not Hexen etc
        if (entry.Path.FullPath.StartsWithIgnoreCase("filter/"))
        {
            string[] validFilterPaths = m_iwadType switch
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

            ParseEntry(entry);
        }
    }

    public void AddAutoBrightmaps(Archive archive)
    {
        foreach (Entry entry in archive.Entries.Where(x => x.Path.FullPath.StartsWithIgnoreCase("brightmaps/auto/")))
        {
            string name = entry.Path.Name;
            BrightMaps.Auto[name] = new BrightmapDefinition() { TargetTexture = name, BrightmapName = name };
        }
    }

    private void ParseEntry(Entry entry)
    {
        m_includeStack.Push(entry.Path.FullPath);
        try
        {

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
        finally
        {
            m_includeStack.Pop();
        }
    }

    private void ParseInclude(Entry entry, SimpleParser parser)
    {
        string path = parser.ConsumeString();
        Entry? includeEntry = entry.Parent.Entries.FirstOrDefault(x => x.Path.FullPath.EqualsIgnoreCase(path));
        if (includeEntry != null)
        {
            if (m_includeStack.Contains(includeEntry.Path.FullPath))
                throw new Exception($"GLDEFS in {entry.Parent.FullPath} contains an infinite loop ({entry.Path.FullPath} -> {includeEntry.Path.FullPath})");
            ParseEntry(includeEntry);
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
                var destination = textureType switch
                {
                    "flat" => BrightMaps.Flats,
                    "sprite" => BrightMaps.Sprites,
                    "texture" => BrightMaps.Textures,
                    _ => null
                };
                destination?.Add(def);
                return;
            }
        }
    }
}
