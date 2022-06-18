using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Helion.Dehacked;
using Helion.Graphics.Fonts;
using Helion.Graphics.Palettes;
using Helion.Maps;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Archives.Iterator;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Data;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Boom;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Images;
using Helion.Resources.IWad;
using Helion.Resources.Textures;
using Helion.Util;
using Helion.Util.Bytes;
using Helion.Util.Configs.Components;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using NLog;

namespace Helion.Resources.Archives.Collection;

/// <summary>
/// A collection of archives along with the processed results of all their
/// data.
/// </summary>
public class ArchiveCollection : IResources
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly DataCache StaticDataCache = new();

    public IWadBaseType IWadType { get; private set; } = IWadBaseType.None;
    public Palette Palette => Data.Palette;
    public IWadInfo IWadInfo => GetIWadInfo();
    public Archive? Assets => m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.Assets);
    public Archive? IWad => m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.IWAD);
    public IEnumerable<Archive> Archives => m_archives.Where(x => x.ArchiveType == ArchiveType.None);
    public AnimatedDefinitions Animdefs => Definitions.Animdefs;
    public BoomAnimatedDefinition BoomAnimated => Definitions.BoomAnimated;
    public BoomSwitchDefinition BoomSwitches => Definitions.BoomSwitches;
    public CompatibilityDefinitions Compatibility => Definitions.Compatibility;
    public DecorateDefinitions Decorate => Definitions.Decorate;
    public FontManager Fonts { get; } = new();
    public IResourceTextureManager Textures { get; }
    public ResourceTracker<TextureDefinition> TextureDefinitions => Definitions.Textures;
    public SoundInfoDefinition SoundInfo => Definitions.SoundInfo;
    public LockDefinitions Locks => Definitions.LockDefininitions;
    public LanguageDefinition Language => Definitions.Language;
    public MapInfoDefinition MapInfo => Definitions.MapInfoDefinition;
    public GameInfoDef GameInfo => Definitions.MapInfoDefinition.GameDefinition;
    public EntityFrameTable EntityFrameTable => Definitions.EntityFrameTable;
    public EntityDefinitionComposer EntityDefinitionComposer { get; }
    public TextureManager TextureManager { get; private set; }
    public DataCache DataCache => StaticDataCache;
    public IImageRetriever ImageRetriever { get; }
    public DehackedDefinition? Dehacked => Definitions.DehackedDefinition;
    public readonly ArchiveCollectionEntries Entries = new();
    public readonly DataEntries Data = new();
    public readonly DefinitionEntries Definitions;
    private readonly IArchiveLocator m_archiveLocator;
    private readonly List<Archive> m_archives = new();
    private readonly Dictionary<string, Font?> m_fonts = new(StringComparer.OrdinalIgnoreCase);
    private string m_lastLoadedMapName = string.Empty;
    private IMap? m_lastLoadedMap;
    private bool m_lastLoadedMapIsTemp;
    private IWadInfo? m_overrideIWadInfo;

    public ArchiveCollection(IArchiveLocator archiveLocator, ConfigCompat config)
    {
        m_archiveLocator = archiveLocator;
        Definitions = new DefinitionEntries(this, config);
        Textures = new ResourceTextureManager(this);
        EntityDefinitionComposer = new EntityDefinitionComposer(this);
        ImageRetriever = new ArchiveImageRetriever(this);
        TextureManager = new TextureManager(this);
    }

    public void InitTextureManager(MapInfoDef mapInfo, bool unitTest = false) =>
        TextureManager = new TextureManager(this, mapInfo, unitTest);

    public Entry? FindEntry(string name, ResourceNamespace? priorityNamespace = null)
    {
        return priorityNamespace == null ?
            Entries.FindByName(name) :
            Entries.FindByNamespace(name, priorityNamespace.Value);
    }

    public Entry? FindEntryByPath(string path)
    {
        return Entries.FindByPath(path);
    }

    public IEnumerable<Entry> GetEntriesByNamespace(ResourceNamespace resourceNamespace)
    {
        return Entries.GetAllByNamespace(resourceNamespace);
    }

    public MapEntryCollection? GetMapEntryCollection(string mapName)
    {
        for (int i = m_archives.Count - 1; i >= 0; i--)
        {
            Archive archive = m_archives[i];
            foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
                if (mapEntryCollection.Name.EqualsIgnoreCase(mapName))
                    return mapEntryCollection;
        }

        return null;
    }

    public IMap? FindMap(string mapName)
    {
        if (m_lastLoadedMapName.EqualsIgnoreCase(mapName) && m_lastLoadedMap != null)
            return m_lastLoadedMap;

        ClearLastLoadedTempMap();

        string mapPathEquals = $"maps/{mapName}.wad";
        string mapPathEnds = $"/maps/{mapName}.wad";

        for (int i = m_archives.Count - 1; i >= 0; i--)
        {
            Archive archive = m_archives[i];
            Entry? mapEntry = archive.Entries.FirstOrDefault(x => x.Path.FullPath.EndsWithIgnoreCase(mapPathEnds) || x.Path.FullPath.EqualsIgnoreCase(mapPathEquals));

            if (mapEntry != null && ExtractAndLoadEmbeddedMapEntry(mapEntry, mapName, out IMap? map))
            {
                m_lastLoadedMapIsTemp = true;
                SetMapLoaded(mapName, map);
                return map;
            }

            foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
            {
                if (!mapEntryCollection.Name.EqualsIgnoreCase(mapName))
                    continue;

                CompatibilityMapDefinition? compat = Definitions.Compatibility.Find(archive, mapName);

                // If we find a map that is corrupt, we want to exit early
                // instead of keep looking since the latest map we find is
                // supposed to override any earlier maps. It would be very
                // confusing to the user in the case where they ask for the
                // most recent map which is corrupt, but then get some
                // earlier map in the pack which is not corrupt.
                map = MapReader.Read(archive, mapEntryCollection, compat);
                if (map != null)
                {
                    m_lastLoadedMapIsTemp = false;
                    SetMapLoaded(mapName, map);
                    return map;
                }

                Log.Warn("Unable to use map {0}, it is corrupt", mapName);
                return null;
            }
        }

        return null;
    }

    private void ClearLastLoadedTempMap()
    {
        if (!m_lastLoadedMapIsTemp || m_lastLoadedMap == null)
            return;

        TempFileManager.DeleteFile(m_lastLoadedMap.Archive.OriginalFilePath);
        m_lastLoadedMap.Archive.Dispose();
        m_lastLoadedMap = null;
        m_lastLoadedMapIsTemp = false;
    }

    private void SetMapLoaded(string mapName, IMap? extractedMap)
    {
        m_lastLoadedMap = extractedMap;
        m_lastLoadedMapName = mapName;
    }

    private bool ExtractAndLoadEmbeddedMapEntry(Entry mapEntry, string mapName, [NotNullWhen(true)] out IMap? map)
    {
        map = null;
        string file = ExtractEmbeddedFile(mapEntry);
        Archive? mapArchive = LoadArchive(file, null);
        if (mapArchive == null)
            return false;

        var mapIterator = new ArchiveMapIterator(mapArchive);
        if (!mapIterator.Any())
        {
            mapArchive.Dispose();
            return false;
        }

        var mapEntryCollection = mapIterator.First();
        CompatibilityMapDefinition? compat = Definitions.Compatibility.Find(mapArchive, mapName);
        map = MapReader.Read(mapArchive, mapEntryCollection, compat);
        mapArchive.Dispose();
        return map != null;
    }

    public Font? GetFont(string name)
    {
        if (m_fonts.TryGetValue(name, out Font? font))
            return font;

        FontDefinition? definition = Definitions.Fonts.Get(name);
        if (definition != null)
        {
            Font? bitmapFont = BitmapFont.From(definition, this);
            m_fonts[name] = bitmapFont;
            return bitmapFont;
        }

        if (Data.TrueTypeFonts.TryGetValue(name, out Font? ttfFont))
        {
            m_fonts[name] = ttfFont;
            return ttfFont;
        }

        return null;
    }

    public Archive? GetArchiveByFileName(string fileName)
    {
        foreach (var archive in m_archives)
            if (Path.GetFileName(archive.OriginalFilePath).EqualsIgnoreCase(fileName))
                return archive;
        return null;
    }

    public void Dispose()
    {
        foreach (var archive in m_archives)
            archive.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool Load(IEnumerable<string> files, string? iwad = null, bool loadDefaultAssets = true, string? dehackedPatch = null, IWadType? iwadTypeOverride = null)
    {
        List<string> filePaths = new();
        Archive? iwadArchive = null;

        if (iwadTypeOverride.HasValue)
            m_overrideIWadInfo = IWadInfo.GetIWadInfo(iwadTypeOverride.Value);

        // If we have nothing loaded, we want to make sure assets.pk3 is
        // loaded before anything else. We also do not want it to be loaded
        // if we have already loaded it.
        if (loadDefaultAssets && m_archives.Empty())
        {
            Archive? assetsArchive = LoadSpecial(Constants.AssetsFileName, ArchiveType.Assets, Files.CalculateMD5(Constants.AssetsFileName));
            if (assetsArchive == null)
                return false;

            m_archives.Add(assetsArchive);
        }

        if (iwad != null)
        {
            iwadArchive = LoadSpecial(iwad, ArchiveType.IWAD, Files.CalculateMD5(iwad));
            if (iwadArchive == null)
                return false;

            m_archives.Add(iwadArchive);
        }

        filePaths.AddRange(files);

        foreach (string filePath in filePaths)
        {
            Archive? archive = LoadArchive(filePath, Files.CalculateMD5(filePath));
            if (archive == null)
                continue;

            m_archives.Add(archive);
        }

        m_archives.AddRange(LoadEmbeddedArchives(m_archives));

        ProcessAndIndexEntries(iwadArchive, m_archives);
        IWadType = GetIWadInfo().IWadBaseType;

        if (loadDefaultAssets)
        {
            // Load all definitions - Even if a map doesn't load them there are cases where they are needed (backpack ammo etc)
            EntityDefinitionComposer.LoadAllDefinitions();
            ApplyDehackedPatch();

            if (dehackedPatch != null)
            {
                try
                {
                    Definitions.ParseDehackedPatch(File.ReadAllText(dehackedPatch));
                    ApplyDehackedPatch();
                }
                catch (IOException)
                {
                    Log.Error($"Unable to open dehacked patch {dehackedPatch}");
                    return false;
                }
            }
        }

        return true;
    }

    private List<Archive> LoadEmbeddedArchives(List<Archive> archives)
    {
        // Loads all embedded wad files, with the exception of wads in a maps/ directory.
        List<string> files = new();
        List<Archive> embeddedArchives = new();
        foreach (var archive in archives)
        {
            if (archive is Wad)
                continue;

            var entries = archive.Entries.Where(x => ShouldExtractWadEntry(archives, x));
            if (!entries.Any())
                continue;

            ExtractEmbeddedFiles(files, entries);
            LoadEmbeddedFiles(files, embeddedArchives, archive);

            files.Clear();
        }

        return embeddedArchives;
    }

    private void LoadEmbeddedFiles(List<string> files, List<Archive> embeddedArchives, Archive archive)
    {
        foreach (string file in files)
        {
            Archive? newArchive = LoadArchive(file, null);
            if (newArchive == null)
                continue;

            newArchive.ExtractedFrom = archive;
            embeddedArchives.Add(newArchive);
        }
    }

    private static bool ShouldExtractWadEntry(List<Archive> archives, Entry entry)
    {
        if (!entry.Path.FullPath.EndsWithIgnoreCase(".wad"))
            return false;

        if (IsEntryInFolder(entry, "maps"))
            return false;

        if (IsEntryInFolder(entry, "autoload"))
        {
            foreach (var archive in archives)
            {
                if (IsEntryInFolder(entry, Path.GetFileName(archive.OriginalFilePath)))
                    return true;
            }

            return false;
        }

        return true;
    }

    private static bool IsEntryInFolder(Entry entry, string folder)
    {
        string folderPath = folder + "/";
        string path = entry.Path.FullPath;
        if (path.StartsWithIgnoreCase(folderPath))
            return true;

        if (!path.GetLastFolder(out var lastFolder))
            return false;

        return lastFolder.Equals(folder, StringComparison.OrdinalIgnoreCase);
    }

    private static void ExtractEmbeddedFiles(List<string> files, IEnumerable<Entry> entries)
    {
        foreach (var entry in entries)
            files.Add(ExtractEmbeddedFile(entry));
    }

    private static string ExtractEmbeddedFile(Entry entry)
    {
        if (entry.IsDirectFile())
            return entry.Path.FullPath;

        string file = TempFileManager.GetFile();
        entry.ExtractToFile(file);
        return file;
    }

    private IWadInfo GetIWadInfo()
    {
        if (m_overrideIWadInfo != null)
            return m_overrideIWadInfo;

        return IWad?.IWadInfo ?? IWadInfo.DefaultIWadInfo;
    }

    private void ApplyDehackedPatch()
    {
        if (Definitions.DehackedDefinition != null)
        {
            DehackedApplier dehackedApplier = new(Definitions, Definitions.DehackedDefinition);
            dehackedApplier.Apply(Definitions.DehackedDefinition, Definitions, EntityDefinitionComposer);
        }
    }

    private Archive? LoadSpecial(string file, ArchiveType archiveType, string? md5)
    {
        Archive? archive = LoadArchive(file, md5);
        if (archive == null)
            return null;

        archive.ArchiveType = archiveType;
        return archive;
    }

    private Archive? LoadArchive(string filePath, string? md5)
    {
        Archive? archive = m_archiveLocator.Locate(filePath);
        if (archive == null)
        {
            Log.Error("Failure when loading {0}", filePath);
            return null;
        }

        archive.OriginalFilePath = filePath;
        if (md5 != null)
            archive.MD5 = md5;

        Log.Info("Loaded {0}", filePath);
        return archive;
    }

    private void ProcessAndIndexEntries(Archive? iwadArchive, List<Archive> archives)
    {
        foreach (Archive archive in archives)
        {
            foreach (Entry entry in archive.Entries)
            {
                Entries.Track(entry);
                Data.Read(entry);
            }

            Definitions.Track(archive);

            if (archive.ArchiveType == ArchiveType.Assets && GetIWadInfo(iwadArchive, out IWadInfo? info))
            {                
                Definitions.LoadMapInfo(archive, info.MapInfoResource);
                Definitions.LoadDecorate(archive, info.DecorateResource);
            }
        }
    }

    private bool GetIWadInfo(Archive? iwadArchive, [NotNullWhen(true)] out IWadInfo? info)
    {
        if (iwadArchive != null)
        {
            iwadArchive.IWadInfo = IWadInfo.GetIWadInfo(iwadArchive.OriginalFilePath);
            info = iwadArchive.IWadInfo;
            return true;
        }

        if (m_overrideIWadInfo != null)
        {
            info = m_overrideIWadInfo;
            return true;
        }

        info = null;
        return false;
    }
}
