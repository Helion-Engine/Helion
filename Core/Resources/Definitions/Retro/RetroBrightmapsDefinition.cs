using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Zdoom;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Resources.Definitions.Retro;

// BRGHTMAPS lump from Doom Retro. Also support by Woof
public class RetroBrightmapsDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, bool[]> m_nameToFullbright = [];
    private readonly Dictionary<BrightmapKey, string> m_textureToBrightmap = [];
    private readonly Dictionary<string, string> m_spriteToBrightmap = [];
    private readonly Dictionary<int, string> m_stateToBrightmap = [];
    private readonly Dictionary<SpriteKey, BrightmapDefinition> m_spriteToBrightmapDefinition = [];

    public bool TryGetFullBright(string name, out bool[]? fullBrightLookup) => m_nameToFullbright.TryGetValue(name, out fullBrightLookup);

    public bool TryGetTextureFullBright(ResourceNamespace type, string name, [NotNullWhen(true)] out bool[]? fullBrightLookup)
    {
        fullBrightLookup = null;
        if (type == ResourceNamespace.Sprites)
        {
            var sprite = name.Length >= 4 ? name.AsSpan(0, 4) : name.AsSpan();
            var lookup = m_spriteToBrightmap.GetAlternateLookup<ReadOnlySpan<char>>();
            if (!lookup.TryGetValue(sprite, out var spriteBrightmapName))
                return false;

            return m_nameToFullbright.TryGetValue(spriteBrightmapName, out fullBrightLookup);
        }

        var key = new BrightmapKey(type, name);
        if (!m_textureToBrightmap.TryGetValue(key, out var brightmapName))
            return false;

        return m_nameToFullbright.TryGetValue(brightmapName, out fullBrightLookup);
    }

    public bool TryGetBrightmapStateName(int vanillaState, [NotNullWhen(true)] out string? brightmapName) => 
        m_stateToBrightmap.TryGetValue(vanillaState, out brightmapName);

    public bool TryGetStateFullBright(int vanillaState, [NotNullWhen(true)] out bool[]? fullBrightLookup)
    {
        fullBrightLookup = null;
        if (!m_stateToBrightmap.TryGetValue(vanillaState, out var brightmapName))
            return false;

        return m_nameToFullbright.TryGetValue(brightmapName, out fullBrightLookup);
    }

    public void Parse(string data, IWadBaseType iwadType)
    {
        var parser = new SimpleParser();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            int startLine = parser.GetCurrentLine();
            var item = parser.ConsumeStringSpan();

            if (item.EqualsIgnoreCase("BRIGHTMAP"))
            {
                ParseBrightMap(parser);
                continue;
            }

            ResourceNamespace type;
            if (item.EqualsIgnoreCase("TEXTURE"))
                type = ResourceNamespace.Textures;
            else if (item.EqualsIgnoreCase("FLAT"))
                type = ResourceNamespace.Flats;
            else if (item.EqualsIgnoreCase("SPRITE"))
                type = ResourceNamespace.Sprites;
            else if (item.EqualsIgnoreCase("STATE"))
            {
                ParseState(iwadType, parser, startLine);
                continue;
            }
            else
            {
                LogError($"Invalid type: {item}");
                continue;
            }
                
            var name = parser.ConsumeString();
            var brightmap = parser.ConsumeString();
            if (!CheckGame(parser, startLine, iwadType))
                continue;

            if (type == ResourceNamespace.Sprites)
                m_spriteToBrightmap[name] = brightmap;
            else
                m_textureToBrightmap[new(type, name)] = brightmap;
        }
    }

    private void ParseState(IWadBaseType iwadType, SimpleParser parser, int startLine)
    {
        var state = parser.ConsumeInteger();
        var brightmap = parser.ConsumeString();
        if (CheckGame(parser, startLine, iwadType))
            m_stateToBrightmap[state] = brightmap;
    }

    private void ParseBrightMap(SimpleParser parser)
    {
        var name = parser.ConsumeString();
        var line = parser.ConsumeLineSpan();
        var indexRanges = line.Split(',');
        var brightmap = new bool[256];

        foreach (var range in indexRanges)
        {
            var rangeSpan = line[range.Start.Value..range.End.Value];
            var dashIndex = rangeSpan.IndexOf('-');
            if (dashIndex == -1)
            {
                if (int.TryParse(rangeSpan, out var index))
                    brightmap[index] = true;
                else
                    LogError($"Invalid index {rangeSpan} for {name}");
                continue;
            }

            var rangeStart = rangeSpan[0..dashIndex];
            var rangeEnd = rangeSpan[(dashIndex + 1)..];

            if (!int.TryParse(rangeStart, out var startIndex) || !int.TryParse(rangeEnd, out var endIndex))
            {
                LogError($"Invalid range {rangeStart}-{rangeEnd} for {name}");
                continue;
            }

            for (int i = startIndex; i <= endIndex; i++)
                brightmap[i] = true;
        }

        m_nameToFullbright[name] = brightmap;
    }

    private static void LogError(string error) => Log.Error($"RetroBrightmap: {error}");

    [Flags]
    private enum GameType
    {
        None = 0,
        Doom1 = 1,
        Doom2 = 2
    }

    private static bool CheckGame(SimpleParser parser, int startLine, IWadBaseType iwadType)
    {
        if (parser.GetCurrentLine() != startLine)
            return true;

        var gameSpan = parser.ConsumeStringSpan();
        var ranges = gameSpan.Split('|');

        var gameType = GameType.None;
        foreach (var range in ranges)
        {
            var game = gameSpan[range.Start.Value..range.End.Value];
            if (game.EqualsIgnoreCase("DOOM") || game.EqualsIgnoreCase("DOOM1"))
                gameType |= GameType.Doom1;
            else if (game.EqualsIgnoreCase("DOOM2"))
                gameType |= GameType.Doom2;
            else
                LogError($"Invalid game type: {game}");
        }

        if (iwadType == IWadBaseType.Doom1)
            return (gameType & GameType.Doom1) != 0;

        if (iwadType == IWadBaseType.Doom2)
            return (gameType & GameType.Doom2) != 0;

        return false;
    }

    public void CreateTextureBrightMaps(ArchiveCollection archiveCollection)
    {
        foreach (var texture in m_textureToBrightmap)
        {
            var image = archiveCollection.ImageRetriever.Get(texture.Key.Name, texture.Key.Namespace);
            if (image == null)
                continue;

            if (!TryGetTextureFullBright(texture.Key.Namespace, texture.Key.Name, out var lookup))
                continue;

            archiveCollection.CreateBrightmap(image, texture.Key.Name, texture.Key.Namespace, lookup);
        }
    }

    public bool TryGetWeaponFullBrightDefinition(ArchiveCollection archiveCollection, int vanillaState, string spriteName, [NotNullWhen(true)] out BrightmapDefinition? brightmap)
    {
        brightmap = null;
        if (vanillaState == 0)
            return false;

        if (!TryGetBrightmapStateName(vanillaState, out var brightMapName))
            return false;

        var spriteKey = new SpriteKey(spriteName, brightMapName);
        if (m_spriteToBrightmapDefinition.TryGetValue(spriteKey, out brightmap))
            return true;

        if (!TryGetStateFullBright(vanillaState, out var fullBrightLookup))
            return false;

        var image = archiveCollection.ImageRetriever.Get(spriteName, ResourceNamespace.Sprites);
        if (image == null)
            return false;

        brightmap = archiveCollection.CreateBrightmap(image, spriteName, ResourceNamespace.Sprites, fullBrightLookup, addToDictionary: false);
        if (brightmap != null)
            m_spriteToBrightmapDefinition[spriteKey] = brightmap;

        return brightmap != null;
    }
}
