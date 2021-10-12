using System;
using System.Collections.Generic;
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
    public ITextureManager Textures { get; }
    public ResourceTracker<TextureDefinition> TextureDefinitions => Definitions.Textures;
    public SoundInfoDefinition SoundInfo => Definitions.SoundInfo;
    public LockDefinitions Locks => Definitions.LockDefininitions;
    public LanguageDefinition Language => Definitions.Language;
    public MapInfoDefinition MapInfo => Definitions.MapInfoDefinition;
    public EntityFrameTable EntityFrameTable => Definitions.EntityFrameTable;
    public EntityDefinitionComposer EntityDefinitionComposer { get; }
    public IImageRetriever ImageRetriever { get; }
    public DehackedDefinition? Dehacked => Definitions.DehackedDefinition;
    public readonly ArchiveCollectionEntries Entries = new();
    public readonly DataEntries Data = new();
    public readonly DefinitionEntries Definitions;
    private readonly IArchiveLocator m_archiveLocator;
    private readonly List<Archive> m_archives = new();
    private readonly Dictionary<string, Font?> m_fonts = new(StringComparer.OrdinalIgnoreCase);

    public ArchiveCollection(IArchiveLocator archiveLocator, ConfigCompat config)
    {
        m_archiveLocator = archiveLocator;
        Definitions = new DefinitionEntries(this, config);
        Textures = new Textures.TextureManager(this);
        EntityDefinitionComposer = new EntityDefinitionComposer(this);
        ImageRetriever = new ArchiveImageRetriever(this);
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
                if (mapEntryCollection.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                    return mapEntryCollection;
        }

        return null;
    }

    public IMap? FindMap(string mapName)
    {
        for (int i = m_archives.Count - 1; i >= 0; i--)
        {
            Archive archive = m_archives[i];
            foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
            {
                if (!mapEntryCollection.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                    continue;

                CompatibilityMapDefinition? compat = Definitions.Compatibility.Find(archive, mapName);

                // If we find a map that is corrupt, we want to exit early
                // instead of keep looking since the latest map we find is
                // supposed to override any earlier maps. It would be very
                // confusing to the user in the case where they ask for the
                // most recent map which is corrupt, but then get some
                // earlier map in the pack which is not corrupt.
                IMap? map = MapReader.Read(archive, mapEntryCollection, compat);
                if (map != null)
                    return map;

                Log.Warn("Unable to use map {0}, it is corrupt", mapName);
                return null;
            }
        }

        return null;
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
            if (Path.GetFileName(archive.OriginalFilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                return archive;
        return null;
    }

    public void Dispose()
    {
        foreach (var archive in m_archives)
        {
            if (archive is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public bool Load(IEnumerable<string> files, string? iwad = null, bool loadDefaultAssets = true, string? dehackedPatch = null)
    {
        List<string> filePaths = new();
        Archive? iwadArchive = null;

        // If we have nothing loaded, we want to make sure assets.pk3 is
        // loaded before anything else. We also do not want it to be loaded
        // if we have already loaded it.
        if (loadDefaultAssets && m_archives.Empty())
        {
            Archive? assetsArchive = LoadSpecial(Constants.AssetsFileName, ArchiveType.Assets);
            if (assetsArchive == null)
                return false;

            m_archives.Add(assetsArchive);
        }

        if (iwad != null)
        {
            iwadArchive = LoadSpecial(iwad, ArchiveType.IWAD);
            if (iwadArchive == null)
                return false;

            m_archives.Add(iwadArchive);
        }

        filePaths.AddRange(files);

        foreach (string filePath in filePaths)
        {
            Archive? archive = LoadArchive(filePath);
            if (archive == null)
                continue;

            m_archives.Add(archive);
        }

        ProcessAndIndexEntries(iwadArchive, m_archives);
        IWadType = GetIWadInfo().IWadBaseType;

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

        return true;
    }

    private IWadInfo GetIWadInfo()
    {
        return IWad?.IWadInfo ?? IWadInfo.DefaultIWadInfo;
    }

    private void ApplyDehackedPatch()
    {
        if (Definitions.DehackedDefinition != null)
        {
            DehackedApplier dehackedApplier = new(Definitions);
            dehackedApplier.Apply(Definitions.DehackedDefinition, Definitions, EntityDefinitionComposer);
        }
    }

    private Archive? LoadSpecial(string file, ArchiveType archiveType)
    {
        Archive? archive = LoadArchive(file);
        if (archive == null)
            return null;

        archive.ArchiveType = archiveType;
        return archive;
    }

    private Archive? LoadArchive(string filePath)
    {
        Archive? archive = m_archiveLocator.Locate(filePath);
        if (archive == null)
        {
            Log.Error("Failure when loading {0}", filePath);
            return null;
        }

        archive.OriginalFilePath = filePath;
        string? md5 = Util.Bytes.Files.CalculateMD5(filePath);
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

            if (archive.ArchiveType == ArchiveType.Assets && iwadArchive != null)
            {
                iwadArchive.IWadInfo = IWadInfo.GetIWadInfo(iwadArchive.OriginalFilePath);
                Definitions.LoadMapInfo(archive, iwadArchive.IWadInfo.MapInfoResource);
                Definitions.LoadDecorate(archive, iwadArchive.IWadInfo.DecorateResource);
            }
        }
    }
}
