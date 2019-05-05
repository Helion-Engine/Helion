using Helion.Entries.Tree.Archive;
using Helion.Entries.Types;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resources;
using Helion.Util;
using NLog;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Entries
{
    /// <summary>
    /// Responsible for classifying entries.
    /// </summary>
    public class EntryClassifier : ResourceClassifier
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, ResourceType> NAME_TO_TYPE = new Dictionary<string, ResourceType>
        {
            ["BEHAVIOR"] = ResourceType.Behavior,
            ["BLOCKMAP"] = ResourceType.Blockmap,
            ["DECORATE"] = ResourceType.Decorate,
            ["LINEDEFS"] = ResourceType.Linedefs,
            ["NODES"] = ResourceType.Nodes,
            ["PLAYPAL"] = ResourceType.Palette,
            ["PNAMES"] = ResourceType.Pnames,
            ["REJECT"] = ResourceType.Reject,
            ["SCRIPTS"] = ResourceType.Scripts,
            ["SECTORS"] = ResourceType.Sectors,
            ["SEGS"] = ResourceType.Segs,
            ["SIDEDEFS"] = ResourceType.Sidedefs,
            ["SSECTORS"] = ResourceType.Subsectors,
            ["THINGS"] = ResourceType.Things,
            ["TEXTURE1"] = ResourceType.TextureX,
            ["TEXTURE2"] = ResourceType.TextureX,
            ["TEXTURE3"] = ResourceType.TextureX,
            ["VERTEXES"] = ResourceType.Vertexes
        };

        private static readonly Dictionary<string, ResourceType> EXTENSION_TO_TYPE = new Dictionary<string, ResourceType>
        {
            ["BMP"] = ResourceType.Image,
            ["JPG"] = ResourceType.Image,
            ["PNG"] = ResourceType.Image,
            ["TIF"] = ResourceType.Image,
            ["TIFF"] = ResourceType.Image,
            ["TTF"] = ResourceType.TrueTypeFont,
            ["TXT"] = ResourceType.Text,
            ["WAD"] = ResourceType.Wad
        };

        private readonly EntryIdAllocator idAllocator;

        public EntryClassifier(EntryIdAllocator allocator) => idAllocator = allocator;

        /// <summary>
        /// Takes the provided data and will turn it into an entry. This 
        /// handles all the hard work of classification and optimizes the
        /// workflow to get the best classification performance possible.
        /// </summary>
        /// <param name="path">The path of the entry.</param>
        /// <param name="data">The raw data that makes up tne entry.</param>
        /// <param name="resourceNamespace">Which namespace this entry will
        /// belong to.</param>
        /// <returns>The entry from the information provided.</returns>
        public Entry ToEntry(EntryPath path, byte[] data, ResourceNamespace resourceNamespace)
        {
            ResourceType resourceType = Classify(path, data, resourceNamespace);

            switch (resourceType)
            {
            case ResourceType.Behavior:
                return new BehaviorEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Blockmap:
                return new BlockmapEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Decorate:
                return new DecorateEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Directory:
                Fail($"Entry classifier should not be finding entries that are directory types");
                break;
            case ResourceType.Linedefs:
                return new LinedefsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Image:
                return new ImageEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Marker:
                return new MarkerEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Nodes:
                return new NodesEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Palette:
                return new PaletteEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.PaletteImage:
                return new PaletteImageEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Pk3:
                Expected<Pk3, string> pk3 = Pk3.FromData(data, path, idAllocator, this);
                if (pk3)
                    return pk3.Value;
                else
                    log.Warn("Error reading nested PK3 at {0}: {1}", path, pk3.Error);
                break;
            case ResourceType.Pnames:
                return new PnamesEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Reject:
                return new RejectEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Scripts:
                return new ScriptsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Sectors:
                return new SectorsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Segs:
                return new SegsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Sidedefs:
                return new SidedefsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Subsectors:
                return new SubsectorsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Things:
                return new ThingsEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Text:
                return new TextEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.TextureX:
                return new TextureXEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.TrueTypeFont:
                return new TrueTypeFontEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Unknown:
                return new UnknownEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Vertexes:
                return new VertexesEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
            case ResourceType.Wad:
                Expected<Wad, string> wad = Wad.FromData(data, path, idAllocator, this);
                if (wad)
                    return wad.Value;
                else
                    log.Warn("Error reading nested wad at {0}: {1}", path, wad.Error);
                break;
            }

            Fail($"Forgot to add resource type {resourceType} to the ToEntry() method");
            return new UnknownEntry(idAllocator.AllocateId(), path, data, resourceNamespace);
        }

        /// <summary>
        /// Classifies the kind of resource type the data is from the arguments
        /// provided.
        /// </summary>
        /// <param name="path">The path in the archive this was found at.
        /// </param>
        /// <param name="data">The raw data that makes up the entry.</param>
        /// <param name="resourceNamespace">The namespace of the entry.</param>
        /// <returns></returns>
        public ResourceType Classify(EntryPath path, byte[] data, ResourceNamespace resourceNamespace)
        {
            // We run into these way more than any other definition, so we give them
            // priority if there's any hints that we're looking at flats or sprites.
            switch (resourceNamespace)
            {
            case ResourceNamespace.Flats:
                if (PaletteReaders.LikelyFlat(data))
                    return ResourceType.PaletteImage;
                break;
            case ResourceNamespace.Sprites:
                if (PaletteReaders.LikelyColumn(data))
                    return ResourceType.PaletteImage;
                break;
            }

            if (NAME_TO_TYPE.TryGetValue(path.Name.ToUpper(), out ResourceType nameResourceType))
                return nameResourceType;

            if (EXTENSION_TO_TYPE.TryGetValue(path.Extension.ToUpper(), out ResourceType extensionResourceType))
                return extensionResourceType;

            if (data.Length == 0)
                return ResourceType.Marker;

            if (ImageReader.CanRead(data, path.Extension))
                return ResourceType.Image;

            if (PaletteReaders.LikelyFlat(data) || PaletteReaders.LikelyColumn(data))
                return ResourceType.PaletteImage;

            return ResourceType.Unknown;
        }
    }
}
