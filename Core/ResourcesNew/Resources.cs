using System;
using System.Collections.Generic;
using Helion.Graphics.Palette;
using Helion.Maps;
using Helion.ResourcesNew.Archives;
using Helion.ResourcesNew.Definitions.Animations;
using Helion.ResourcesNew.Definitions.Compatibility;
using Helion.ResourcesNew.Definitions.Decorate;
using Helion.ResourcesNew.Definitions.Fonts;
using Helion.ResourcesNew.Definitions.SoundInfo;
using Helion.ResourcesNew.Definitions.Textures;
using Helion.ResourcesNew.Sprites;
using Helion.ResourcesNew.Textures;
using Helion.Util;
using Helion.Util.Configuration;
using NLog;

namespace Helion.ResourcesNew
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
        private readonly Config m_config;
        private readonly List<Archive> m_archives = new();
        private readonly Dictionary<CIString, Entry> m_nameToEntry = new();
        private readonly TextureDefinitionManager m_textureDefinitionManager = new();

        public Resources(Config config, bool loadAssets = true)
        {
            m_config = config;
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

        public IMap? FindMap(CIString name)
        {
            // TODO
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
                    LoadDefinitionFile(entry);
                }
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
                break;
            case "TEXTURE1":
            case "TEXTURE2":
            case "TEXTURE3":
                break;
            case "SNDINFO":
                break;
            }
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
