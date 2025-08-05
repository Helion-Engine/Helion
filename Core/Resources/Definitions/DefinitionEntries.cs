using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Dehacked;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Boom;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Fonts;
using Helion.Resources.Definitions.Id24;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.MusInfo;
using Helion.Resources.Definitions.Retro;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Definitions.Zdoom;
using Helion.Resources.IWad;
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World.Entities.Definition;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions;

/// <summary>
/// All the text-based entries that have been parsed into usable data
/// structures.
/// </summary>
public class DefinitionEntries
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
    public readonly OptionsDefinition OptionsDefinition = new();
    public readonly MusInfoDefinition MusInfoDefinition = new();
    public readonly Id24SkyDefinition Id24SkyDefinition = new();
    public readonly Id24TranslationDefinition Id24TranslationDefinition = new();

    /// <inheritdoc cref="Id24.GameConfDefinition"/>
    public readonly GameConfDefinition GameConfDefinition = new();

    /// <inheritdoc cref="Zdoom.GameInfoDefinition"/>
    public readonly GameInfoDefinition GameInfoDefinition = new();

    /// <inheritdoc cref="Zdoom.GldefsDefinition"/>
    public readonly GldefsDefinition GldefsDefinition = new();

    public RetroBrightmapsDefinition? RetroBrightmapsDefinition;

    public PnamesTextureXCollection PnamesTextureXCollection => m_pnamesTextureXCollection;
    public DehackedDefinition? DehackedDefinition { get; set; }
    public LookupArray<Colormap> BloodColorMaps => m_bloodColorMaps;

    private readonly Dictionary<string, Action<Entry>> m_entryNameToAction = new(StringComparer.OrdinalIgnoreCase);
    private readonly ArchiveCollection m_archiveCollection;
    private readonly Dictionary<string, Colormap> m_processedTranslationColormaps = [];
    private readonly PnamesTextureXCollection m_pnamesTextureXCollection = new();
    private readonly LookupArray<Colormap> m_bloodColorMaps = new();
    private bool m_parseDehacked;
    private bool m_parseDecorate;
    private bool m_parseZDoomMapInfo;
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
        m_entryNameToAction["SNDINFO"] = entry => ParseEntry(ParseSoundInfo, entry);
        m_entryNameToAction["LANGUAGE"] = entry => ParseEntry(ParseLanguage, entry);
        m_entryNameToAction["LANGUAGECOMPAT"] = entry => ParseEntry(ParseLanguageCompatibility, entry);
        m_entryNameToAction["MAPINFO"] = entry => ParseEntry(ParseMapInfo, entry);
        m_entryNameToAction["ZMAPINFO"] = entry => ParseEntry(ParseZMapInfo, entry);
        m_entryNameToAction["UMAPINFO"] = entry => ParseEntry(ParseUniversalMapInfo, entry);
        m_entryNameToAction["DEHACKED"] = entry => ParseEntry(ParseDehacked, entry);
        m_entryNameToAction["TEXTURES"] = entry => ParseEntry(ParseTextures, entry);
        m_entryNameToAction["COMPLVL"] = entry => ParseEntry(ParseCompLevel, entry);
        m_entryNameToAction["OPTIONS"] = OptionsDefinition.Parse;
        m_entryNameToAction["MUSINFO"] = entry => ParseEntry(ParseMusInfo, entry);
        m_entryNameToAction["SKYDEFS"] = Id24SkyDefinition.Parse;
        m_entryNameToAction["GAMECONF"] = GameConfDefinition.Parse;
        m_entryNameToAction["GAMEINFO"] = entry => ParseEntry(GameInfoDefinition.Parse, entry);
        m_entryNameToAction["GLDEFS"] = ParseGldefs;
        m_entryNameToAction["DOOMDEFS"] = ParseGldefs;
        m_entryNameToAction["BRGHTMPS"] = ParseRetroBrightmaps;
    }

    private void ParseRetroBrightmaps(Entry entry)
    {
        RetroBrightmapsDefinition ??= new();
        RetroBrightmapsDefinition.Parse(entry.ReadDataAsString(), m_archiveCollection.IWadInfo.IWadBaseType);
    }

    public void ParseDehackedPatch(string data)
    {
        DehackedDefinition ??= new();
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
    private void ParseLanguage(string text) => Language.Parse(text, m_archiveCollection.IWadInfo);
    private void ParseLanguageCompatibility(string text) => Language.ParseCompatibility(text);
    private void ParseCompLevel(string data) => CompLevelDefinition.Parse(data);
    private void ParseMusInfo(string text) => MusInfoDefinition.Parse(text);
    private void ParseUniversalMapInfo(string text) => MapInfoDefinition.ParseUniversalMapInfo(m_archiveCollection.IWadInfo.IWadBaseType, text);
    private void ParseGldefs(Entry entry) => GldefsDefinition.Parse(entry, m_archiveCollection.IWadInfo.IWadBaseType);

    private void ParseZMapInfo(string text)
    {
        if (!m_parseZDoomMapInfo)
            return;

        MapInfoDefinition.Parse(m_archiveCollection, text, ShouldParseWeapons);
    }

    private void ParseMapInfo(string text)
    {
        if (!m_parseLegacyMapInfo)
            return;

        MapInfoDefinition.Parse(m_archiveCollection, text, ShouldParseWeapons);
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

    public void Track(Archive archive)
    {
        m_parseDecorate = true;
        m_parseDehacked = true;
        m_parseZDoomMapInfo = true;
        m_parseLegacyMapInfo = true;

        bool hasBoth = archive.AnyEntryByName("DEHACKED") && archive.AnyEntryByName("DECORATE");
        if (ConfigCompatibility.PreferDehacked && hasBoth)
            m_parseDecorate = false;
        else if (!ConfigCompatibility.PreferDehacked && hasBoth)
            m_parseDehacked = false;

        if (archive.AnyEntryByName("ZMAPINFO"))
            m_parseLegacyMapInfo = false;

        // Prioritize UMAPINFO when SKYDEFS is present since MAPINFO can conflict with SKYDEFS.
        bool skyDefs = archive.AnyEntryByName("SKYDEFS");
        bool umapInfo = archive.AnyEntryByName("UMAPINFO");
        if (umapInfo && skyDefs)
        {
            m_parseZDoomMapInfo = false;
            m_parseLegacyMapInfo = false;
        }

        foreach (Entry entry in archive.Entries)
        {
            if (m_entryNameToAction.TryGetValue(entry.Path.Name, out var action))
                action(entry);
            if (entry.Namespace == ResourceNamespace.Colormaps)
                AddColormap(entry);
        }

        // Vanilla IWADS will have this set. If a PWAD is loaded this will get clear it.
        ConfigCompatibility.VanillaShortestTexture.Set(archive.IWadInfo.VanillaCompatibility);

        GldefsDefinition.AddAutoBrightmaps(archive);
    }

    public void Finalize(ArchiveCollection archiveCollection)
    {
        AddFinalizeEntry(archiveCollection, "PNAMES", m_pnamesTextureXCollection.AddPnames);
        AddFinalizeEntry(archiveCollection, "TEXTURE1", m_pnamesTextureXCollection.AddTextureX);
        AddFinalizeEntry(archiveCollection, "TEXTURE2", m_pnamesTextureXCollection.AddTextureX);
        AddFinalizeEntry(archiveCollection, "TEXTURE3", m_pnamesTextureXCollection.AddTextureX);

        CreateImageDefinitionsFrom(archiveCollection, m_pnamesTextureXCollection);

        RetroBrightmapsDefinition?.CreateTextureBrightMaps(m_archiveCollection);
    }

    private static void AddFinalizeEntry(ArchiveCollection archiveCollection, string name, Action<Entry> action)
    {
        var entry = archiveCollection.FindEntry(name);
        if (entry != null)
            action(entry);
    }

    public void BuildTranslationColorMaps(Palette palette, Colormap baseColorMap)
    {
        if (GetGameConfPlayerTranslations(out var playerColormaps))
        {
            var translatedColorMaps = new List<Colormap>(Colormaps.Count + playerColormaps.Length);
            foreach (var playerColormap in playerColormaps)
            {
                if (playerColormap == null)
                {
                    translatedColorMaps.Add(baseColorMap);
                    continue;
                }
                translatedColorMaps.Add(playerColormap);
            }
            translatedColorMaps.AddRange(Colormaps);
            Colormaps.Clear();
            Colormaps.AddRange(translatedColorMaps);
        }
        else
        {
            SetPlayerColorMaps(palette, baseColorMap);
        }

        for (int i = 0; i < Colormaps.Count; i++)
            Colormaps[i].Index = i;

        SetGameConfTranslations();

        if (DehackedDefinition != null)
            SetEntityTranslations(DehackedDefinition);

        m_processedTranslationColormaps.Clear();
    }

    private bool GetGameConfPlayerTranslations([NotNullWhen(true)] out Colormap?[]? colormaps)
    {
        var translations = GameConfDefinition?.Data?.PlayerTranslations;
        if (translations == null || translations.Length != 4)
        {
            colormaps = null;
            return false;
        }

        colormaps = new Colormap?[4];
        for (int i = 0; i < translations.Length; i++)
        {
            if (!TryParseTranslationEntryToColormap(translations[i], false, out var colormap))
                continue;
            colormaps[i] = colormap;
        }

        return true;
    }

    private void SetPlayerColorMaps(Palette palette, Colormap baseColorMap)
    {
        if (m_archiveCollection.Data.ColormapData.Length == 0)
            return;

        var colormapBytes = m_archiveCollection.Data.ColormapData;
        int colorCount = (int)TranslateColor.Count;
        // First player colormap is default
        List<Colormap> translatedColormaps = new(Colormaps.Count + colorCount + 1)
        {
            baseColorMap
        };
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

        // Translated player colormaps must be first
        translatedColormaps.AddRange(Colormaps);
        Colormaps.Clear();
        Colormaps.AddRange(translatedColormaps);

        if (DehackedDefinition != null && DehackedDefinition.HasBloodColor)
            CreateBloodColorMaps(palette, colormapBytes, DehackedDefinition.BloodColors);
    }

    private void CreateBloodColorMaps(Palette palette, byte[] colormapBytes, IEnumerable<PaletteColor> paletteColors)
    {
        foreach (var paletteColor in paletteColors)
        {
            var colormap = Colormap.TranslateToNearestMatch(palette, colormapBytes, paletteColor);
            if (colormap == null)
                continue;

            m_bloodColorMaps.Set((int)paletteColor, colormap);
            Colormaps.Add(colormap);
        }
    }

    public Colormap GetBloodColormap(PaletteColor color)
    {
        if (m_bloodColorMaps.TryGetValue((int)color, out var bloodColorMap))
            return bloodColorMap;

        return Colormaps[0];
    }

    private void SetGameConfTranslations()
    {
        var gameConf = GameConfDefinition.Data;
        if (gameConf == null)
            return;

        if (string.IsNullOrEmpty(gameConf.WadTranslation) || !TryParseTranslationEntryToPalette(gameConf.WadTranslation, out var palette))
            return;

        if (m_archiveCollection.IWad != null)
            m_archiveCollection.IWad.TranslationPalette = palette;

        HashSet<string> wadFiles = new(StringComparer.OrdinalIgnoreCase);
        if (gameConf.Pwads != null)
        {
            foreach (var file in gameConf.Pwads)
                wadFiles.Add(file);
        }

        foreach (var archive in m_archiveCollection.Archives)
        {
            if (wadFiles.Contains(archive.Path.NameWithExtension))
                archive.TranslationPalette = palette;
        }
    }

    private void SetEntityTranslations(DehackedDefinition dehacked)
    {
        HashSet<string> translationEntries = [];
        Dictionary<string, List<EntityDefinition>> translationDefinitions = [];
        var definitions = m_archiveCollection.EntityDefinitionComposer.GetEntityDefinitions();
        foreach (var definition in definitions)
        {
            var entryName = definition.Properties.TranslationEntry;
            if (string.IsNullOrEmpty(entryName))
                continue;

            if (!translationDefinitions.TryGetValue(entryName, out var list))
            {
                list = [];
                translationDefinitions[entryName] = list;
            }

            list.Add(definition);
            translationEntries.Add(entryName);
        }

        foreach (var thing in dehacked.Things)
        {
            if (string.IsNullOrEmpty(thing.TranslationLump))
                continue;

            translationEntries.Add(thing.TranslationLump);
        }

        foreach (var entryName in translationEntries)
        {
            if (!TryParseTranslationEntryToColormap(entryName, true, out var colormap))
                continue;

            m_processedTranslationColormaps[entryName] = colormap;

            colormap.Index = Colormaps.Count;
            Colormaps.Add(colormap);

            if (!translationDefinitions.TryGetValue(entryName, out var list))
                continue;

            foreach (var entityDef in list)
                entityDef.Properties.ColormapIndex = colormap.Index;
        }
    }

    private bool TryParseTranslationEntryToColormap(string entryName, bool addToColorMaps, [NotNullWhen(true)] out Colormap? colormap)
    {
        if (addToColorMaps && m_processedTranslationColormaps.TryGetValue(entryName, out colormap))
            return true;

        colormap = null;
        if (!TryParseTranslationEntry(entryName, out _, out var translationDef))
            return false;

        colormap = Colormap.CreateTranslatedColormap(m_archiveCollection.Data.Palette, m_archiveCollection.Data.ColormapData, translationDef.Data.Table);
        if (colormap == null)
            return false;

        m_processedTranslationColormaps[entryName] = colormap;

        if (addToColorMaps)
        {
            colormap.Index = Colormaps.Count;
            Colormaps.Add(colormap);
        }

        return true;
    }

    private bool TryParseTranslationEntryToPalette(string entryName, [NotNullWhen(true)] out Palette? palette)
    {
        palette = null;
        if (!TryParseTranslationEntry(entryName, out _, out var translationDef))
            return false;

        palette = Palette.CreateTranslatedPalette(m_archiveCollection.Data.Palette, translationDef.Data.Table);
        return palette != null;
    }

    private bool TryParseTranslationEntry(string entryName, [NotNullWhen(true)] out Entry? translationEntry, [NotNullWhen(true)] out TranslationDef? translationDef)
    {
        translationDef = null;
        translationEntry = m_archiveCollection.Entries.FindByName(entryName);
        if (translationEntry == null)
        {
            LogTranslationNotFound("entry", entryName);
            return false;
        }

        translationDef = Id24TranslationDefinition.Parse(translationEntry);
        if (translationDef == null)
        {
            LogTranslationNotFound("definition", entryName);
            return false;
        }

        return true;
    }

    private static void LogTranslationNotFound(string type, string entryName)
    {
        Log.Error($"Translation {type} not found for {entryName}");
    }

    private void AddColormap(Entry entry)
    {
        var colormap = Colormap.From(m_archiveCollection.Data.Palette, entry.ReadData(), entry);
        if (colormap != null)
        {
            if (entry.Parent.ArchiveType == ArchiveType.Assets && entry.Path.Name.EqualsIgnoreCase("WATERMAP"))
                colormap.ColorMix = (0, 4, 165); //ZDoom uses 0004FA5. This is for true color rendering only.

            colormap.Index = Colormaps.Count;
            Colormaps.Add(colormap);
            ColormapsLookup[entry.Path.Name] = colormap;
        }
    }

    private void CreateImageDefinitionsFrom(ArchiveCollection archiveCollection, PnamesTextureXCollection collection)
    {
        var processed = new HashSet<string>();
        foreach (var archive in archiveCollection.Archives)
        {
            if (archive is not Wad wadArchive)
                continue;

            var ns = ResourceNamespace.Textures;
            foreach (var textureEntry in wadArchive.TxEntries)
            {
                var name = textureEntry.Path.Name;
                var component = new TextureDefinitionComponent(name, Vec2I.Zero);
                var def = new TextureDefinition(name, (0, 0), ns, [component], isAutoImageTexture: true);
                ProcessTextureDefinition(archiveCollection, processed, def);
            }
        }

        if (collection.Valid)
        {
            // Note: We don't handle multiple pnames. I am not sure how they're
            // handled, it might be 'one pnames to textureX' when more than one
            // pnames exist. If so, the logic will need to change here a bit.
            var pnames = collection.Pnames.First();
            Precondition(!collection.Pnames.Empty(), "Expecting pnames to exist when reading TextureX definitions");
            foreach (var textureX in collection.TextureX)
            {
                var textureDefinitions = textureX.ToTextureDefinitions(pnames);
                foreach (var def in textureDefinitions)
                    ProcessTextureDefinition(archiveCollection, processed, def);
            }
        }
    }

    private void ProcessTextureDefinition(ArchiveCollection archiveCollection, HashSet<string> processed, TextureDefinition def)
    {
        // Ignore duplicated textures from same archive
        // E.g. Ancient Aliens has KS_FLSG6 duplicated and using the second texture breaks animated range values.
        if (!processed.Add(def.Name))
            return;

        ClearNegativePatchOffsets(archiveCollection, def);
        Textures.Insert(def.Name, def.Namespace, def);
    }

    private static void ClearNegativePatchOffsets(ArchiveCollection archiveCollection, TextureDefinition texture)
    {
        if (archiveCollection.IWadInfo.IWadBaseType == IWadBaseType.Doom1)
            ClearPatchOffsetsDoom1(texture);
    }

    private static void ClearPatchOffsetsDoom1(TextureDefinition texture)
    {
        var patches = texture.Components;
        if (texture.Name.EqualsIgnoreCase("SKY1"))
        {
            if (patches.Count == 1 && patches[0].Offset.Y == -8)
                patches[0].Offset.Y = 0;
        }
        else if (texture.Name.EqualsIgnoreCase("BIGDOOR7"))
        {
            if (patches.Count == 2 && patches[0].Offset.Y == -4 && patches[1].Offset.Y == -4)
            {
                patches[0].Offset.Y = 0;
                patches[1].Offset.Y = 0;
            }
        }
    }
}
