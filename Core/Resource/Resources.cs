using System;
using System.Collections.Generic;
using Helion.Graphics.Palette;
using Helion.Maps;
using Helion.Resource.Archives;
using Helion.Resource.Definitions.Animations;
using Helion.Resource.Definitions.Compatibility;
using Helion.Resource.Definitions.Decorate;
using Helion.Resource.Definitions.Fonts;
using Helion.Resource.Definitions.SoundInfo;
using Helion.Resource.Definitions.Textures;
using Helion.Resource.Sprites;
using Helion.Resource.Tracker;
using Helion.Util;
using NLog;
using TextureManager = Helion.Resource.Textures.TextureManager;

namespace Helion.Resource
{
    /// <summary>
    /// All of the loaded resources from archives thus far.
    /// </summary>
    public class Resources : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly AnimationManager Animations = new();
        public readonly CompatibilityManager Compatibility = new();
        public readonly DecorateManager Decorate;
        public readonly FontManager Fonts;
        public Palette Palette = Palettes.GetDefaultPalette();
        public readonly SoundInfoManager Sounds;
        public readonly SpriteManager Sprites;
        public readonly TextureManager Textures;
        private readonly List<Archive> m_archives = new();
        private readonly Dictionary<CIString, Entry> m_nameToEntry = new();
        private readonly NamespaceTracker<Entry> m_entryTracker = new();
        private readonly TextureDefinitionManager m_textureDefinitionManager = new();

        public Resources(bool loadAssets = true)
        {
            Sounds = new(this);
            Textures = new(this, m_textureDefinitionManager);
            Sprites = new(this, Textures);
            Fonts = new(this, Textures);
            Decorate = new(this, Textures, Sprites);

            if (loadAssets)
                Load(Constants.AssetsFileName);
        }

        /// <summary>
        /// Loads a file. See <see cref="Load(System.Collections.Generic.IEnumerable{string})"/>.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns>True on success, false on failure.</returns>
        public bool Load(string path) => Load(new[] { path });

        /// <summary>
        /// Loads all the files from the paths provided. If one or more fail,
        /// then none of the archives are loaded. Failure may result in a state
        /// of missing resources.
        /// </summary>
        /// <param name="paths">The paths to the files.</param>
        /// <returns>True on success, false if any failed.</returns>
        public bool Load(IEnumerable<string> paths)
        {
            List<Archive> archives = new();

            foreach (string path in paths)
            {
                Archive? archive = Archive.Open(path);
                if (archive == null)
                {
                    Log.Error("Unable to open archive at: {0}", path);
                    return false;
                }

                archives.Add(archive);
            }

            ProcessLoadedArchives(archives);
            return true;
        }

        public Entry? Find(CIString name)
        {
            return m_nameToEntry.TryGetValue(name, out Entry? entry) ? entry : null;
        }

        public Entry? Find(CIString name, Namespace resourceNamespace)
        {
            return m_entryTracker.GetOnly(name, resourceNamespace);
        }

        public Map? FindMap(CIString name)
        {
            for (int i = m_archives.Count - 1; i >= 0; i--)
            {
                Archive archive = m_archives[i];

                // The reason we early out on the first map we find that has a
                // name match is because we don't want to confuse the user if
                // we ignore corruption and keep looking for a valid one. For
                // example, suppose there's a few archives with MAP01. It would
                // be weird if the latest one is corrupt, and we return a map
                // from an earlier one. Exiting early lets the user know this.
                foreach (MapEntryCollection mapEntries in new ArchiveMapIterator(archive))
                    if (mapEntries.Name == name)
                        return Map.Read(mapEntries);
            }

            return null;
        }

        private void ProcessLoadedArchives(IEnumerable<Archive> archives)
        {
            foreach (Archive archive in archives)
            {
                m_archives.Add(archive);

                foreach (Entry entry in archive)
                {
                    m_nameToEntry[entry.Path.Name] = entry;
                    m_entryTracker.Insert(entry.Path.Name, entry.Namespace, entry);
                    LoadDefinitionFile(entry);
                }

                FinishProcessingArchive();
            }
        }

        private void LoadDefinitionFile(Entry entry)
        {
            switch (entry.Path.Name.ToUpper())
            {
            case "ANIMDEFS":
                break;
            case "COMPATIBILITY":
                break;
            case "DECORATE":
                break;
            case "FONTS":
                break;
            case "PLAYPAL":
                LoadPlaypal(entry);
                break;
            case "PNAMES":
                m_textureDefinitionManager.AddPnames(entry);
                break;
            case "TEXTURE1":
            case "TEXTURE2":
            case "TEXTURE3":
                m_textureDefinitionManager.AddTextureX(entry);
                break;
            case "SNDINFO":
                break;
            }
        }

        private void FinishProcessingArchive()
        {
            // The definition manager needs to be told when we're done because
            // it will not know if more TEXTUREx or PNAMES are coming. There
            // are wads that have multiple definitions and have them placed in
            // an unusual order. Vanilla (and thus ZDoom and friends) also have
            // continued to use the unintuitive handling of PNAMES/TEXTUREx.
            m_textureDefinitionManager.NotifyArchiveFinished();
        }

        private void LoadPlaypal(Entry entry)
        {
            Palette? palette = Palette.From(entry.ReadData());
            if (palette != null)
                Palette = palette;
            else
                Log.Error("Unable to load palette, data is corrupt");
        }

        public void Dispose()
        {
            m_archives.ForEach(a => a.Dispose());

            m_archives.Clear();
            m_nameToEntry.Clear();
        }
    }
}
