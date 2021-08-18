using Helion.Dehacked;
using Helion.Graphics.Palettes;
using Helion.Maps;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Boom;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.IWad;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using System;
using System.Collections.Generic;
using Helion.Graphics.Fonts;
using Helion.Resources.Textures;
using Helion.Resources.Images;

namespace Helion.Resources
{
    /// <summary>
    /// A collection of all the resources that are loaded.
    /// </summary>
    public interface IResources : IDisposable
    {
        IWadBaseType IWadType { get; }
        Palette Palette { get; }
        IWadInfo IWadInfo { get; }
        Archive? Assets { get; }
        Archive? IWad { get; }
        IEnumerable<Archive> Archives { get; }
        AnimatedDefinitions Animdefs { get; }
        BoomAnimatedDefinition BoomAnimated { get; }
        BoomSwitchDefinition BoomSwitches { get; }
        CompatibilityDefinitions Compatibility { get; }
        DecorateDefinitions Decorate { get; }
        FontManager Fonts { get; }
        ITextureManager Textures { get; }
        SoundInfoDefinition SoundInfo { get; }
        LockDefinitions Locks { get; }
        LanguageDefinition Language { get; }
        MapInfoDefinition MapInfo { get; }
        EntityFrameTable EntityFrameTable { get; }
        EntityDefinitionComposer EntityDefinitionComposer { get; }
        DehackedDefinition? Dehacked { get; }
        ArchiveImageRetriever ImageRetriever { get; }

        Entry? FindEntry(string name, ResourceNamespace? priorityNamespace = null);
        Entry? FindEntryByPath(string path);
        IEnumerable<Entry> GetEntriesByNamespace(ResourceNamespace resourceNamespace);
        MapEntryCollection? GetMapEntryCollection(string mapName);
        IMap? FindMap(string mapName);
        Font? GetFont(string name);
        Archive? GetArchiveByFileName(string fileName);
    }
}
