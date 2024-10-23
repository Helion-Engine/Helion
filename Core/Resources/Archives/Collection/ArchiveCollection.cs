using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Helion.Dehacked;
using Helion.Graphics.Fonts;
using Helion.Graphics.Palettes;
using Helion.Maps;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
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
using Helion.Resources.Definitions.Id24;
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
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using NLog;

namespace Helion.Resources.Archives.Collection;

/// <summary>
/// A collection of archives along with the processed results of all their
/// data.
/// </summary>
public class ArchiveCollection : IResources, IPathResolver
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public event EventHandler<Archive>? ArchiveLoaded;
    public event EventHandler<Archive>? ArchiveRead;

    public static readonly DataCache StaticDataCache = new();

    public IWadBaseType IWadType { get; private set; } = IWadBaseType.None;
    public Palette Palette => Data.Palette;
    public Colormap Colormap => Data.Colormap;
    public IWadInfo IWadInfo => GetIWadInfo();
    public Archive? Assets => m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.Assets);
    public Archive? IWad => m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.IWAD);
    public IEnumerable<Archive> Archives => m_archives.Where(x => x.ArchiveType == ArchiveType.None);
    public IEnumerable<Archive> AllArchives => m_archives;
    public AnimatedDefinitions Animdefs => Definitions.Animdefs;
    public BoomAnimatedDefinition BoomAnimated => Definitions.BoomAnimated;
    public BoomSwitchDefinition BoomSwitches => Definitions.BoomSwitches;
    public CompatibilityDefinitions Compatibility => Definitions.Compatibility;
    public DecorateDefinitions Decorate => Definitions.Decorate;
    public FontManager Fonts { get; } = new();
    public IResourceTextureManager Textures { get; }
    public ResourceTracker<TextureDefinition> TextureDefinitions => Definitions.Textures;
    public SoundInfoDefinition SoundInfo => Definitions.SoundInfo;
    public LockDefinitions Locks => Definitions.LockDefinitions;
    public LanguageDefinition Language => Definitions.Language;
    public MapInfoDefinition MapInfo => Definitions.MapInfoDefinition;
    public GameInfoDef GameInfo => Definitions.MapInfoDefinition.GameDefinition;
    public EntityFrameTable EntityFrameTable => Definitions.EntityFrameTable;
    public EntityDefinitionComposer EntityDefinitionComposer { get; }
    public TextureManager TextureManager { get; private set; }
    public DataCache DataCache { get; }
    public IImageRetriever ImageRetriever { get; }
    public bool Loaded { get; private set; }
    public bool StoreImageIndices => ShaderVars.PaletteColorMode;
    public IConfig Config => m_config;
    public DehackedDefinition? Dehacked => Definitions.DehackedDefinition;
    public ArchiveCollectionEntries Entries = new();
    public DataEntries Data = new();
    public DefinitionEntries Definitions;
    private readonly IArchiveLocator m_archiveLocator;
    private readonly List<Archive> m_archives = new();
    private readonly Dictionary<string, Font?> m_fonts = new(StringComparer.OrdinalIgnoreCase);
    private readonly IConfig m_config;
    private string m_lastLoadedMapName = string.Empty;
    private IMap? m_lastLoadedMap;
    private bool m_lastLoadedMapIsTemp;
    private bool m_initTextureManager;

    public ArchiveCollection(IArchiveLocator archiveLocator, Config config, DataCache dataCache)
    {
        m_archiveLocator = archiveLocator;
        Definitions = new DefinitionEntries(this, config.Compatibility);
        Textures = new ResourceTextureManager(this, config);
        EntityDefinitionComposer = new EntityDefinitionComposer(this);
        ImageRetriever = new ArchiveImageRetriever(this);
        TextureManager = new TextureManager(this);
        DataCache = dataCache;
        m_config = config;
    }

    public void InitTextureManager(MapInfoDef mapInfo, bool unitTest = false)
    {
        if (unitTest)
        {
            TextureManager = new TextureManager(this, m_config.Render.CacheSprites, unitTest);
            SetTextureManagerSky(mapInfo);
            return;
        }

        if (m_initTextureManager)
        {
            SetTextureManagerSky(mapInfo);
            TextureManager.MapInit();
            return;
        }

        TextureManager = new TextureManager(this, m_config.Render.CacheSprites, unitTest);
        SetTextureManagerSky(mapInfo);
        m_initTextureManager = true;
    }

    private void SetTextureManagerSky(MapInfoDef mapInfo)
    {
        if (mapInfo.Sky1.Name != null && mapInfo.Sky1.Name.Length > 0)
            TextureManager.SetSkyTexture(mapInfo.Sky1.Name ?? Constants.DefaultSkyTextureName);
        else
            TextureManager.SetSkyTexture(Constants.DefaultSkyTextureName);
    }

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
                map = IMap.Read(archive, mapEntryCollection, compat);
                if (map != null)
                {
                    m_lastLoadedMapIsTemp = false;
                    SetMapLoaded(mapName, map);
                    return map;
                }

                HelionLog.Warn($"Unable to use map {mapName}, it is corrupt");
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
        Archive? mapArchive = LoadArchive(file);
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
        map = IMap.Read(mapArchive, mapEntryCollection, compat);
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
            Font? bitmapFont = BitmapFont.From(definition, this, m_config.Hud.FontUpscalingFactor);
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

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
        foreach (var archive in m_archives)
            archive.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool Load(IEnumerable<string> files, string? iwad = null, bool loadDefaultAssets = true, string? dehackedPatch = null, Archive? iwadOverride = null, bool checkGameConfArchives = false)
    {
        if (Loaded)
        {
            foreach (var archive in m_archives)
                archive.Dispose();
            m_archives.Clear();

            Entries = new();
            Data = new();
            Definitions = new(this, m_config.Compatibility);
        }

        Loaded = true;
        List<string> filePaths = [];
        Archive? iwadArchive = null;

        // If we have nothing loaded, we want to make sure assets.pk3 is
        // loaded before anything else. We also do not want it to be loaded
        // if we have already loaded it.
        if (loadDefaultAssets && m_archives.Empty())
        {
            Archive? assetsArchive = LoadSpecial(Constants.AssetsFileName, ArchiveType.Assets, shouldCalculateMd5: true);
            if (assetsArchive == null)
                return false;

            m_archives.Add(assetsArchive);
        }

        if (checkGameConfArchives)
            m_archives.AddRange(LoadGameConfArchives(iwad, files));

        if (iwadOverride != null)
        {
            iwadArchive = iwadOverride;
            m_archives.Add(iwadArchive);
        }
        else if (iwad != null)
        {
            iwadArchive = LoadSpecial(iwad, ArchiveType.IWAD, shouldCalculateMd5: true);
            if (iwadArchive == null)
                return false;

            m_archives.Add(iwadArchive);
        }

        filePaths.AddRange(files);

        foreach (string filePath in filePaths)
        {
            Archive? archive = LoadArchive(filePath, shouldCalculateMd5: true);
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
            EntityFrameTable.AddCustomFrames();

            if (iwad != null || files.Any())
                Definitions.BuildTranslationColorMaps(Data.Palette, Data.Colormap);

            if (dehackedPatch != null)
            {
                try
                {
                    Definitions.ParseDehackedPatch(File.ReadAllText(dehackedPatch));
                    ApplyDehackedPatch();
                }
                catch (IOException)
                {
                    HelionLog.Error($"Unable to open dehacked patch {dehackedPatch}");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Adds required resource WADs to the WAD list
    /// if a GAMECONF is present in the specified WADs
    /// </summary>
    private List<Archive> LoadGameConfArchives(string? iwad, IEnumerable<string> pwads)
    {
        List<string> wads = [];
        if (iwad != null)
            wads.Add(iwad);
        wads.AddRange(pwads);

        // parse GAMECONFs in the specified WADs
        GameConfDefinition gameConfDef = new();
        foreach (string wad in wads)
        {
            Archive? archive = LoadArchive(wad, isLoadEvent: false);
            var entry = archive?.GetEntryByName("GAMECONF");
            if (entry != null)
                gameConfDef.Parse(entry);
        }

        // add whichever archives are needed
        List<Archive> gameConfArchives = [];
        if (gameConfDef.Data?.Executable == GameConfConstants.Executable.Id24)
        {
            const string Id24ResName = "id24res.wad";
            Archive? id24ResArchive = LoadArchive(Id24ResName, shouldCalculateMd5: true);
            if (id24ResArchive == null)
                HelionLog.Error($"Unable to open {Id24ResName} for ID24 config");
            else
                gameConfArchives.Add(id24ResArchive);
        }
        return gameConfArchives;
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
            Archive? newArchive = LoadArchive(file);
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
        return IWad?.IWadInfo ?? IWadInfo.DefaultIWadInfo;
    }

    public void ApplyDehackedPatch()
    {
        if (Definitions.DehackedDefinition != null)
        {
            Definitions.DehackedDefinition.LoadActorDefinitions(EntityDefinitionComposer);
            DehackedApplier dehackedApplier = new(Definitions, Definitions.DehackedDefinition);
            dehackedApplier.Apply(Definitions.DehackedDefinition, Definitions, EntityDefinitionComposer);
        }
    }

    private Archive? LoadSpecial(string file, ArchiveType archiveType, bool shouldCalculateMd5 = false)
    {
        Archive? archive = LoadArchive(file, shouldCalculateMd5);
        if (archive == null)
            return null;

        archive.ArchiveType = archiveType;
        return archive;
    }

    private Archive? LoadArchive(string filePath, bool shouldCalculateMd5 = false, bool isLoadEvent = true)
    {
        Archive? archive = m_archiveLocator.Locate(filePath);
        if (archive == null)
        {
            Log.Error($"Failure when loading {filePath}");
            return null;
        }

        archive.OriginalFilePath = Path.GetFullPath(filePath);
        if (shouldCalculateMd5)
        {
            string? md5 = Files.CalculateMD5(archive.Path.FullPath);
            if (md5 != null)
                archive.MD5 = md5;
        }

        if (isLoadEvent)
            ArchiveLoaded?.Invoke(this, archive);
        else
            ArchiveRead?.Invoke(this, archive);

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

    private static bool GetIWadInfo(Archive? iwadArchive, [NotNullWhen(true)] out IWadInfo? info)
    {
        if (iwadArchive != null)
        {
            iwadArchive.IWadInfo = IWadInfo.GetIWadInfo(iwadArchive.OriginalFilePath);
            info = iwadArchive.IWadInfo;
            return true;
        }

        info = null;
        return false;
    }

    /// <summary>
    /// ID24 GAMECONF is allowed to define an IWAD and additional PWADs.
    /// If an IWAD is specified, it will override any previously specified one.
    /// PWADs added by PWADs are placed before the PWAD that added them.
    /// </summary>
    public (string? iwad, List<string> pwads) GetWadsFromGameConfs(string? originalIwad, List<string> originalPwads)
    {
        string? iwad = originalIwad;
        List<string> pwads = [];

        string? LocateReferencedWad(string wadName, string? referencingWadPath)
        {
            // first check in the same folder
            var siblingPath = Path.Join(referencingWadPath, wadName);
            if (Path.Exists(siblingPath))
                return siblingPath;
            // check in other search paths
            else
            {
                string? otherPath = m_archiveLocator.LocateWithoutLoading(wadName);
                if (otherPath != null)
                    return otherPath;
            }
            return null;
        }

        GameConfDefinition parser = new();
        void ApplyWadsFromWadGameConf(string wad)
        {
            using var archive = LoadArchive(wad);
            var entry = archive?.GetEntryByName("GAMECONF");
            if (entry == null)
                return;
            parser.Data = null;
            parser.Parse(entry);
            if (parser.Data == null)
                return;
            // assume that referenced PWADs are in the same directory before
            // falling back to other search paths, the spec isn't clear here
            string? wadDir = Path.GetDirectoryName(wad);
            if (parser.Data.Iwad != null)
            {
                // don't replace an iwad by the same name, so that users can specify a version if they want
                string? previousIwadName = Path.GetFileName(iwad);
                if (previousIwadName == null || !parser.Data.Iwad.EqualsIgnoreCase(previousIwadName))
                {
                    string? locatedIwad = LocateReferencedWad(parser.Data.Iwad, wadDir);
                    if (locatedIwad != null)
                        iwad = locatedIwad;
                }
            }
            if (parser.Data.Pwads != null)
            {
                foreach (string pwad in parser.Data.Pwads)
                {
                    string? locatedPwad = LocateReferencedWad(pwad, wadDir);
                    if (locatedPwad != null)
                        pwads.Add(locatedPwad);
                }
            }
        }

        if (originalIwad != null)
        {
            ApplyWadsFromWadGameConf(originalIwad);
        }
        foreach (string pwad in originalPwads)
        {
            pwads.Add(pwad);
            ApplyWadsFromWadGameConf(pwad);
        }

        return (iwad, pwads);
    }
}
