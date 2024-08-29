using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.Fonts;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Util.Extensions;
using NLog;
using Helion.Util.Parser;
using Helion.Resources.Definitions.Boom;
using Helion.Dehacked;
using Helion.World.Entities.Definition;
using Helion.Util.Configs.Components;
using static Helion.Util.Assertion.Assert;
using Helion.Graphics.Palettes;
using Helion.Resources.Definitions.MusInfo;
using Helion.Resources.Definitions.Id24;
using Helion.Resources.IWad;

namespace Helion.Resources.Definitions;

/// <summary>
/// All the text-based entries that have been parsed into usable data
/// structures.
/// </summary>
public class DefinitionEntries
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<IWadBaseType, HashSet<string>> IgnorePatchOffsets = new()
    {
        {IWadBaseType.Doom1, new HashSet<string>(["SKY1"], StringComparer.OrdinalIgnoreCase)}
    };

    public readonly AnimatedDefinitions Animdefs = new();
    public readonly BoomAnimatedDefinition BoomAnimated = new();
    public readonly BoomSwitchDefinition BoomSwitches = new();
    public readonly CompatibilityDefinitions Compatibility = new();
    public readonly DecorateDefinitions Decorate;
    public readonly FontDefinitionCollection Fonts = new();
    public readonly ResourceTracker<TextureDefinition> Textures = new();
    public readonly SoundInfoDefinition SoundInfo = new();
    public readonly LockDefinitions LockDefinitions = new();
    public readonly LanguageDefinition Language = new();
    public readonly MapInfoDefinition MapInfoDefinition = new();
    public readonly ConfigCompat ConfigCompatibility;
    public readonly EntityFrameTable EntityFrameTable = new();
    public readonly TexturesDefinition TexturesDef = new();
    public readonly Dictionary<string, Colormap> ColormapsLookup = [];
    public readonly List<Colormap> Colormaps = [];
    public readonly CompLevelDefinition CompLevelDefinition = new();
    public readonly MusInfoDefinition MusInfoDefinition = new();
    public readonly Id24SkyDefinition Id24SkyDefinition = new();

    public DehackedDefinition? DehackedDefinition { get; set; }

    private readonly Dictionary<string, Action<Entry>> m_entryNameToAction = new(StringComparer.OrdinalIgnoreCase);
    private readonly ArchiveCollection m_archiveCollection;
    private PnamesTextureXCollection m_pnamesTextureXCollection = new();
    private bool m_parseDehacked;
    private bool m_parseDecorate;
    private bool m_parseUniversalMapInfo;
    private bool m_parseLegacyMapInfo;
    private bool m_parseWeapons;

    private bool ShouldParseWeapons => m_parseWeapons || !ConfigCompatibility.PreferDehacked;

    /// <summary>
    /// Creates a definition entries data structure which has no tracked
    /// data.
    /// </summary>
    public DefinitionEntries(ArchiveCollection archiveCollection, ConfigCompat config)
    {
        m_archiveCollection = archiveCollection;
        ConfigCompatibility = config;
        Decorate = new DecorateDefinitions(archiveCollection);

        m_entryNameToAction["ANIMATED"] = entry => BoomAnimated.Parse(entry);
        m_entryNameToAction["SWITCHES"] = entry => BoomSwitches.Parse(entry);
        m_entryNameToAction["ANIMDEFS"] = entry => ParseEntry(ParseAnimDefs, entry);
        m_entryNameToAction["COMPATIBILITY"] = entry => Compatibility.AddDefinitions(entry);
        m_entryNameToAction["DECORATE"] = entry => ParseDecorate(entry);
        m_entryNameToAction["FONTS"] = entry => Fonts.AddFontDefinitions(entry);
        m_entryNameToAction["PNAMES"] = entry => m_pnamesTextureXCollection.AddPnames(entry);
        m_entryNameToAction["TEXTURE1"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
        m_entryNameToAction["TEXTURE2"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
        m_entryNameToAction["TEXTURE3"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
        m_entryNameToAction["SNDINFO"] = entry => ParseEntry(ParseSoundInfo, entry);
        m_entryNameToAction["LANGUAGE"] = entry => ParseEntry(ParseLanguage, entry);
        m_entryNameToAction["LANGUAGECOMPAT"] = entry => ParseEntry(ParseLanguageCompatibility, entry);
        m_entryNameToAction["MAPINFO"] = entry => ParseEntry(ParseMapInfo, entry);
        m_entryNameToAction["ZMAPINFO"] = entry => ParseEntry(ParseZMapInfo, entry);
        m_entryNameToAction["UMAPINFO"] = entry => ParseEntry(ParseUniversalMapInfo, entry);
        m_entryNameToAction["DEHACKED"] = entry => ParseEntry(ParseDehacked, entry);
        m_entryNameToAction["TEXTURES"] = entry => ParseEntry(ParseTextures, entry);
        m_entryNameToAction["COMPLVL"] = entry => ParseEntry(ParseCompLevel, entry);
        m_entryNameToAction["MUSINFO"] = entry => ParseEntry(ParseMusInfo, entry);
        m_entryNameToAction["SKYDEFS"] = Id24SkyDefinition.Parse;
    }

    public void ParseDehackedPatch(string data)
    {
        if (DehackedDefinition == null)
            DehackedDefinition = new();

        DehackedDefinition.Parse(data);
    }

    public bool LoadMapInfo(Archive archive, string entryName)
    {
        if (!GetEntry(archive, entryName, out Entry? entry) || entry == null)
            return false;

        m_parseWeapons = true;
        ParseEntry(ParseMapInfo, entry);
        m_parseWeapons = false;
        return true;
    }

    public bool LoadDecorate(Archive archive, string entryName)
    {
        if (!GetEntry(archive, entryName, out Entry? entry) || entry == null)
            return false;

        Decorate.AddDecorateDefinitions(entry);
        return true;
    }

    private static bool GetEntry(Archive archive, string entryName, out Entry? entry)
    {
        if (entryName.Length == 0)
            entry = null;
        else
            entry = archive.Entries.FirstOrDefault(x => x.Path.FullPath.Equals(entryName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            Log.Error($"Failed to find resource {entryName}");
            return false;
        }

        return true;
    }

    private void ParseAnimDefs(string text) => Animdefs.Parse(text);
    private void ParseSoundInfo(string text) => SoundInfo.Parse(text);
    private void ParseLanguage(string text) => Language.Parse(text);
    private void ParseLanguageCompatibility(string text) => Language.ParseCompatibility(text);
    private void ParseZMapInfo(string text) => MapInfoDefinition.Parse(m_archiveCollection, text, ShouldParseWeapons);
    private void ParseCompLevel(string data) => CompLevelDefinition.Parse(data);
    private void ParseMusInfo(string text) => MusInfoDefinition.Parse(text);

    private void ParseMapInfo(string text)
    {
        if (!m_parseLegacyMapInfo)
            return;

        MapInfoDefinition.Parse(m_archiveCollection, text, ShouldParseWeapons);
    }

    private void ParseUniversalMapInfo(string text)
    {
        if (!m_parseUniversalMapInfo)
            return;

        MapInfoDefinition.ParseUniversalMapInfo(MapInfoDefinition.MapInfo, text);
    }

    private void ParseTextures(string text)
    {
        TexturesDef.Parse(text);
        foreach (var texture in TexturesDef.Textures)
            Textures.Insert(texture.Name, ResourceNamespace.Textures, texture);
    }

    private void ParseDehacked(string text)
    {
        if (!m_parseDehacked)
            return;

        ParseDehackedPatch(text);
    }

    private void ParseDecorate(Entry entry)
    {
        if (!m_parseDecorate)
            return;

        Decorate.AddDecorateDefinitions(entry);
    }

    private static void ParseEntry(Action<string> parseAction, Entry entry)
    {
        string text = entry.ReadDataAsString();

        try
        {
            parseAction(text);
        }
        catch (ParserException e)
        {
            var logMessages = e.LogToReadableMessage(text);
            foreach (var message in logMessages)
                Log.Error(message);
            // TODO this hard crashes with no dialog
            //throw;
        }
    }

    /// <summary>
    /// Tracks all the resources from an archive.
    /// </summary>
    /// <param name="archive">The archive to examine for any texture
    /// definitions.</param>
    public void Track(Archive archive)
    {
        m_parseDecorate = true;
        m_parseDehacked = true;
        m_parseUniversalMapInfo = true;
        m_parseLegacyMapInfo = true;

        bool hasBoth = archive.AnyEntryByName("DEHACKED") && archive.AnyEntryByName("DECORATE");
        if (ConfigCompatibility.PreferDehacked && hasBoth)
            m_parseDecorate = false;
        else if (!ConfigCompatibility.PreferDehacked && hasBoth)
            m_parseDehacked = false;

        var hasZmapinfo = archive.AnyEntryByName("ZMAPINFO");
        if (hasZmapinfo)
            m_parseLegacyMapInfo = false;

        if (hasZmapinfo || archive.AnyEntryByName("MAPINFO"))
            m_parseUniversalMapInfo = false;

        m_pnamesTextureXCollection = new PnamesTextureXCollection();

        foreach (Entry entry in archive.Entries)
        {
            if (m_entryNameToAction.TryGetValue(entry.Path.Name, out var action))
                action(entry);
            if (entry.Namespace == ResourceNamespace.Colormaps)
                AddColormap(entry);
        }

        if (m_pnamesTextureXCollection.Valid)
            CreateImageDefinitionsFrom(archive, m_pnamesTextureXCollection);

        // Vanilla IWADS will have this set. If a PWAD is loaded this will get clear it.
        ConfigCompatibility.VanillaShortestTexture.Set(archive.IWadInfo.VanillaCompatibility);
    }


    public void BuildTranslationColorMaps(Palette palette, Colormap baseColorMap)
    {
        if (baseColorMap.Entry == null)
            return;

        var colormapBytes = baseColorMap.Entry.ReadData();
        int colorCount = (int)TranslateColor.Count;
        List<Colormap> translatedColormaps = new(Colormaps.Count + colorCount);
        // Doom built 3 translation color maps that map green to gray, brown, and red
        for (int i = 0; i < colorCount; i++)
        {
            var colormap = Colormap.CreateTranslatedColormap(palette, colormapBytes, (TranslateColor)i);
            if (colormap == null)
            {
                Log.Error("Failed to create translation colormap.");
                continue;
            }
            translatedColormaps.Add(colormap);
        }

        // Translated colormaps must be first
        translatedColormaps.AddRange(Colormaps);
        Colormaps.Clear();
        Colormaps.AddRange(translatedColormaps);
        for (int i = 0; i < Colormaps.Count; i++)
            Colormaps[i].Index = i;
    }

    private void AddColormap(Entry entry)
    {
        var colormap = Colormap.From(m_archiveCollection.Data.Palette, entry.ReadData(), entry);
        if (colormap != null)
        {
            colormap.Index = Colormaps.Count;
            Colormaps.Add(colormap);
            ColormapsLookup[entry.Path.Name] = colormap;
        }
    }

    private void CreateImageDefinitionsFrom(Archive archive, PnamesTextureXCollection collection)
    {
        Precondition(!collection.Pnames.Empty(), "Expecting pnames to exist when reading TextureX definitions");

        HashSet<string> ignoreNames;
        if (IgnorePatchOffsets.TryGetValue(archive.IWadInfo.IWadBaseType, out var getIgnoreNames))
            ignoreNames = getIgnoreNames;
        else
            ignoreNames = [];

        // Note: We don't handle multiple pnames. I am not sure how they're
        // handled, it might be 'one pnames to textureX' when more than one
        // pnames exist. If so, the logic will need to change here a bit.
        Pnames pnames = collection.Pnames.First();
        var processed = new HashSet<string>();
        foreach (var textureX in collection.TextureX)
        {
            var textureDefinitions = textureX.ToTextureDefinitions(pnames, ignoreNames);
            foreach (var def in textureDefinitions)
            {
                // Ignore duplicated textures from same archive
                // E.g. Ancient Aliens has KS_FLSG6 duplicated and using the second texture breaks animated range values.
                if (processed.Contains(def.Name))
                    continue;

                processed.Add(def.Name);
                Textures.Insert(def.Name, def.Namespace, def);
            }
        }
    }
}
